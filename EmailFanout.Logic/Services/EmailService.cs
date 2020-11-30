using EmailFanout.Logic.Config;
using EmailFanout.Logic.Models;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic.Services
{
    public class EmailService : IEmailService
    {
        private readonly IStatusService _statusService;
        private readonly IKeyVaultHelper _keyVaultHelper;
        private readonly IConfigService _configService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IHttpClient _httpClient;

        public EmailService(
            IStatusService statusService,
            IKeyVaultHelper keyVaultHelper,
            IConfigService configService,
            IBlobStorageService blobStorageService,
            IHttpClient httpClient)
        {
            _statusService = statusService;
            _configService = configService;
            _blobStorageService = blobStorageService;
            _httpClient = httpClient;
            _keyVaultHelper = keyVaultHelper;
        }

        public async Task<bool> ProcessMailAsync(EmailRequest request, CancellationToken cancellationToken)
        {
            var existingEntries = await _statusService.GetStatiAsync(request, cancellationToken);
            var config = await _configService.LoadAsync(cancellationToken);

            var tasks = Task.WhenAll(ActionsToPerform(request.Email, config).Select(async action =>
            {
                try
                {
                    // check if mail/action combination was already processed before (and partially/fully succeeded)
                    // needed because we let sendgrid handle the retry
                    if (existingEntries.ContainsKey(action.Id) &&
                        existingEntries[action.Id].GetStatus() == EmailFanoutStatus.Completed)
                    {
                        // this email has already been successfully delivered to the target
                        return;
                    }

                    await ProcessAsync(request, action, cancellationToken);
                    await _statusService.UpdateAsync(request, action, EmailFanoutStatus.Completed, cancellationToken);
                }
                catch
                {
                    await _statusService.UpdateAsync(request, action, EmailFanoutStatus.DeferredOrFailed, cancellationToken);
                    throw;
                }
            }));
            try
            {
                await tasks;
            }
            catch (Exception)
            {
                // only first exception is throw, bubble up the aggregated exception
                throw tasks.Exception;
            }
            // is this even possible ?
            if (tasks.Exception != null)
                throw tasks.Exception;

            return true;
        }

        private async Task ProcessAsync(EmailRequest request, EmailAction action, CancellationToken cancellationToken)
        {
            var properties = action.Properties;

            switch (action.Type)
            {
                case ActionType.Archive:
                    // one folder per day is fine for now 
                    var id = $"{request.Timestamp:yyyy-MM}/{request.Timestamp:dd}/{request.Timestamp:HH-mm-ss}_{request.Email.From.Email} - {request.Email.Subject}";

                    var name = $"{id}.json";
                    var containerName = action.Properties.Property("containerName").Value.ToString();
                    await _blobStorageService.UploadAsync(containerName, name, JsonConvert.SerializeObject(request.Email, Formatting.Indented), cancellationToken);
                    // save all attachments in subfolder
                    var tasks = Task.WhenAll(request.Email.Attachments.Select(a => _blobStorageService.UploadAsync(containerName, $"{id} (Attachments)/{a.FileName}", Convert.FromBase64String(a.Base64Data), cancellationToken)));
                    await tasks;
                    if (tasks.Exception != null)
                        throw tasks.Exception;

                    break;
                case ActionType.Forward:
                    {
                        var secretName = action.Properties.Property("webhook")?.Value?.ToObject<SecretObject>()?.SecretName ?? throw new KeyNotFoundException($"Could not find secretName of webhook in action {action.Id}");
                        var webhookUrl = await _keyVaultHelper.GetSecretAsync(secretName, cancellationToken);

                        request.Body.Position = 0;
                        var r = await _httpClient.PostStreamAsync(webhookUrl, request.Body, cancellationToken);
                        if (r.StatusCode != HttpStatusCode.OK &&
                            r.StatusCode != HttpStatusCode.Accepted)
                        {
                            throw new WebhookException($"Failed calling webhook {action.Id}");
                        }
                    }
                    break;
                case ActionType.Webhook:
                    {
                        var secretName = action.Properties.Property("webhook")?.Value?.ToObject<SecretObject>()?.SecretName ?? throw new KeyNotFoundException($"Could not find secretName of webhook in action {action.Id}");
                        var webhookUrl = await _keyVaultHelper.GetSecretAsync(secretName, cancellationToken);

                        string Format(string text) => text
                            .Replace("%sender%", request.Email.From.Email)
                            .Replace("%subject%", request.Email.Subject)
                            .Replace("%body%", request.Email.Text ?? request.Email.Html)
                            .Replace("\"%attachments%\"", request.Email.Attachments != null ? JsonConvert.SerializeObject(request.Email.Attachments) : "null");

                        var json = action.Properties.Property("body")?.Value?.ToString();
                        json = Format(json);

                        var r = await _httpClient.PostAsync(webhookUrl, json, cancellationToken);
                        if (r.StatusCode != HttpStatusCode.OK &&
                            r.StatusCode != HttpStatusCode.Accepted)
                        {
                            throw new WebhookException($"Failed calling webhook {action.Id}");
                        }
                    }
                    break;
                case ActionType.Email:
                    {
                        var secretName = action.Properties.Property("sendgrid")?.Value?.ToObject<SecretObject>()?.SecretName ?? throw new KeyNotFoundException($"Could not find secretName of email in action {action.Id}");
                        var domain = action.Properties.Property("domain")?.Value?.ToString() ?? throw new KeyNotFoundException($"Could not find domain of email in action {action.Id}");
                        var targetEmail = action.Properties.Property("targetEmail")?.Value?.ToString() ?? throw new KeyNotFoundException($"Could not find targetEmail of email in action {action.Id}");

                        var sendgridKey = await _keyVaultHelper.GetSecretAsync(secretName, cancellationToken);

                        var sendgridClient = new SendGridClient(sendgridKey);
                        // only add section if CC emails exist
                        var prefix = "";
                        if (request.Email.Cc.Length > 0)
                        {
                            var newLine = "\r\n";
                            if (!string.IsNullOrEmpty(request.Email.Html))
                                newLine += "<br>";

                            if (request.Email.Cc.Any())
                                prefix += $"CC: {string.Join("; ", request.Email.Cc.Select(x => x.Email))}";
                            prefix += newLine + "__________";
                        }

                        // mail can be sent to multiple targets - our domain may not be the first and may even appear multiple times (same address or various domain addresses)
                        // -> match first with domain name
                        var emails = request.Email.To.Select(e => e.Email)
                            .Concat((request.Email.Cc ?? new EmailAddress[0]).Select(c => c.Email))
                            .ToArray();

                        domain = domain.StartsWith("@") ? domain.Substring(1) : domain;
                        var fromEmail = emails.FirstOrDefault(e => e.EndsWith($"@{domain}", StringComparison.InvariantCultureIgnoreCase)) ?? $"unknown@{domain}";
                        string EnsureNotEmpty(string message)
                        {
                            // pgp signed emails have no content but attachments only;
                            // seems to be this bug
                            // https://github.com/sendgrid/sendgrid-nodejs/issues/435
                            return string.IsNullOrEmpty(message) ? " " : message;
                        }

                        var mail = MailHelper.CreateSingleEmail(new EmailAddress(fromEmail), new EmailAddress(targetEmail), request.Email.Subject, EnsureNotEmpty(prefix + request.Email.Text), EnsureNotEmpty(prefix + request.Email.Html));
                        // causes the reply button to magically replace "fromEmail" with the email of the original author -> respond gets sent to the correct person
                        mail.ReplyTo = request.Email.From;
                        foreach (var attachment in request.Email.Attachments)
                        {
                            mail.AddAttachment(new Attachment
                            {
                                ContentId = attachment.ContentId,
                                Content = attachment.Base64Data,
                                Filename = attachment.FileName,
                                Type = attachment.ContentType
                            });
                        }
                        var response = await sendgridClient.SendEmailAsync(mail, cancellationToken);
                        if (response.StatusCode != HttpStatusCode.Accepted)
                        {
                            var errorResponse = await response.Body.ReadAsStringAsync();
                            throw new BadRequestException($"Sendgrid did not accept. The response was: {response.StatusCode}." + Environment.NewLine + errorResponse);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unsupported type {action.Type}");
            }
        }

        public static EmailAction[] ActionsToPerform(Email mail, EmailConfig config)
        {
            var actions = new List<EmailAction>();
            foreach (var rule in config.Rules)
            {
                if (!rule.Enabled)
                    continue;
                bool deliver = true;
                foreach (var filter in rule.Filters ?? new EmailFilter[0])
                {
                    if (!filter.Enabled)
                        continue;

                    if (!IsMatchedByFilter(mail, filter))
                    {
                        deliver = false;
                        break;
                    }
                }
                if (!deliver)
                    continue;

                actions.AddRange(rule.Actions.Where(a => a.Enabled));
            }
            return actions.ToArray();
        }

        public static bool IsMatchedByFilter(Email mail, EmailFilter filter)
        {
            bool MatchAny(string text)
                => text != null && filter.OneOf.Any(item => text.IndexOf(item, StringComparison.OrdinalIgnoreCase) >= 0);
            bool EqualsAny(string text)
                => text != null && filter.OneOf.Any(item => text.Equals(item, StringComparison.OrdinalIgnoreCase));
            bool NotMatchAll(string text)
                => text != null && filter.AllOf.All(item => text.IndexOf(item, StringComparison.OrdinalIgnoreCase) < 0);
            bool NotEqualsAll(string text)
                => text != null && filter.AllOf.All(item => !text.Equals(item, StringComparison.OrdinalIgnoreCase));

            switch (filter.Type.ToLowerInvariant())
            {
                case "sender contains":
                    return MatchAny(mail.From.Name) || MatchAny(mail.From.Email);
                case "sender equals":
                    return EqualsAny(mail.From.Name) || EqualsAny(mail.From.Email);
                case "!sender contains":
                    return NotMatchAll(mail.From.Name) && NotMatchAll(mail.From.Email);
                case "!sender equals":
                    return NotEqualsAll(mail.From.Name) && NotEqualsAll(mail.From.Email);
                case "subject contains":
                    return MatchAny(mail.Subject);
                case "!subject contains":
                    return NotMatchAll(mail.Subject);
                case "body contains":
                    return MatchAny(mail.Text ?? mail.Html);
                case "!body contains":
                    return NotMatchAll(mail.Text ?? mail.Html);
                case "subject/body contains":
                    return MatchAny(mail.Subject) || MatchAny(mail.Text ?? mail.Html);
                case "!subject/body contains":
                    return NotMatchAll(mail.Subject) && NotMatchAll(mail.Text ?? mail.Html);
                case "recipient contains":
                    return mail.To.Any(to => MatchAny(to.Name) || MatchAny(to.Email)) ||
                        mail.Cc.Any(to => MatchAny(to.Name) || MatchAny(to.Email));
                case "!recipient contains":
                    return mail.To.Any(to => NotMatchAll(to.Name) && NotMatchAll(to.Email)) ||
                        mail.Cc.Any(to => NotMatchAll(to.Name) && NotMatchAll(to.Email));
                case "recipient equals":
                    return mail.To.Any(to => EqualsAny(to.Name) || EqualsAny(to.Email)) ||
                        mail.Cc.Any(to => EqualsAny(to.Name) || EqualsAny(to.Email));
                case "!recipient equals":
                    return mail.To.Any(to => NotEqualsAll(to.Name) && NotEqualsAll(to.Email)) ||
                        mail.Cc.Any(to => NotEqualsAll(to.Name) && NotEqualsAll(to.Email));
                default:
                    throw new ArgumentOutOfRangeException($"Unsupported {filter.Type}");
            }
        }

        private class SecretObject
        {
            public string SecretName { get; set; }
        }
    }
}
