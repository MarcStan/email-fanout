using Azure.Security.KeyVault.Secrets;
using EmailFanout.Logic.Config;
using EmailFanout.Logic.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Logic.Services
{
    public class EmailService : IEmailService
    {
        private readonly IStatusService _statusService;
        private readonly SecretClient _secretClient;
        private readonly IConfigService _configService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IHttpClient _httpClient;

        public EmailService(
            IStatusService statusService,
            SecretClient secretClient,
            IConfigService configService,
            IBlobStorageService blobStorageService,
            IHttpClient httpClient)
        {
            _statusService = statusService;
            _secretClient = secretClient;
            _configService = configService;
            _blobStorageService = blobStorageService;
            _httpClient = httpClient;
        }

        public async Task<bool> ProcessMailAsync(EmailRequest request, CancellationToken cancellationToken)
        {
            var existingEntries = await _statusService.GetStatiAsync(request, cancellationToken);
            var config = await _configService.LoadAsync(cancellationToken);

            var errors = new List<Exception>();
            foreach (var action in ActionsToPerform(request.Email, config))
            {
                try
                {
                    // check if mail/action combination was already processed before (and partially/fully succeeded)
                    // needed because we let sendgrid handle the retry
                    if (existingEntries.ContainsKey(action.Id) &&
                        existingEntries[action.Id].GetStatus() == EmailFanoutStatus.Completed)
                    {
                        // this email has already been successfully delivered to the target
                        continue;
                    }

                    await ProcessAsync(request, action, cancellationToken);
                    await _statusService.UpdateAsync(request, action, EmailFanoutStatus.Completed, cancellationToken);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                    await _statusService.UpdateAsync(request, action, EmailFanoutStatus.DeferredOrFailed, cancellationToken);
                }
            }
            if (errors.Count > 0)
                throw new AggregateException("Failed processing mail ", errors);

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
                    await Task.WhenAll(request.Email.Attachments.Select(a => _blobStorageService.UploadAsync(containerName, $"{id} (Attachments)/{a.FileName}", Convert.FromBase64String(a.Base64Data), cancellationToken)));
                    break;
                case ActionType.Forward:
                    {
                        var secretName = action.Properties.Property("webhook").ToObject<Webhook>().SecretName;
                        var webhookSecret = await _secretClient.GetSecretAsync(secretName, null, cancellationToken);

                        request.Body.Position = 0;
                        var r = await _httpClient.PostAsync(webhookSecret.Value.Value, request.Body, cancellationToken);
                    }
                    break;
                case ActionType.Webhook:
                    {
                        var secretName = action.Properties.Property("webhook").ToObject<Webhook>().SecretName;
                        var webhookSecret = await _secretClient.GetSecretAsync(secretName, null, cancellationToken);

                        string Format(string text) => text
                            .Replace("%sender%", request.Email.From.Email)
                            .Replace("%subject%", request.Email.Subject)
                            .Replace("%body%", request.Email.Text ?? request.Email.Html);

                        var obj = new
                        {
                            sender = Format(action.Properties.Property("sender")?.Value?.ToString() ?? request.Email.From.Email),
                            subject = Format(action.Properties.Property("subject")?.Value?.ToString() ?? request.Email.Subject),
                            body = Format(action.Properties.Property("body")?.Value?.ToString() ?? request.Email.Text ?? request.Email.Html),
                            attachments = "keep".Equals(action.Properties.Property("attachments")?.Value?.ToString(), StringComparison.OrdinalIgnoreCase) ?
                                request.Email.Attachments :
                                null
                        };
                        var r = await _httpClient.PostAsync(webhookSecret.Value.Value, obj, cancellationToken);
                    }
                    break;
            }
        }

        private EmailAction[] ActionsToPerform(Email mail, EmailConfig config)
        {
            var actions = new List<EmailAction>();
            foreach (var rule in config.Rules)
            {
                bool deliver = true;
                foreach (var filter in rule.Filters ?? new EmailFilter[0])
                {
                    if (!IsMatchedByFilter(mail, filter))
                    {
                        deliver = false;
                        break;
                    }
                }
                if (!deliver)
                    continue;

                actions.AddRange(rule.Actions);
            }
            return actions.ToArray();
        }

        private bool IsMatchedByFilter(Email mail, EmailFilter filter)
        {
            bool MatchAny(string text)
                => text != null && filter.OneOf.Any(item => text.Contains(item));

            switch (filter.Type.ToLowerInvariant())
            {
                case "sender contains":
                    return MatchAny(mail.From.Name) || MatchAny(mail.From.Email);
                case "subject contains":
                    return MatchAny(mail.Subject);
                case "body contains":
                    return MatchAny(mail.Text ?? mail.Html);
                case "subject/body contains":
                    return MatchAny(mail.Subject) || MatchAny(mail.Text ?? mail.Html);
                case "recipient contains":
                    return mail.To.Any(to => MatchAny(to.Name) || MatchAny(to.Email)) ||
                        mail.Cc.Any(to => MatchAny(to.Name) || MatchAny(to.Email));
                default:
                    throw new ArgumentOutOfRangeException($"Unsupported {filter.Type}");
            }
        }
        private class Webhook
        {
            public string SecretName { get; set; }
        }
    }
}
