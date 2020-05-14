using EmailFanout.Logic;
using EmailFanout.Logic.Config;
using EmailFanout.Logic.Models;
using EmailFanout.Logic.Services;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Tests
{
    public class EmailServiceTests
    {
        [Test]
        public async Task Service_should_invoke_multiple_actions()
        {
            var status = new Mock<IStatusService>();
            var vault = new Mock<IKeyVaultHelper>();
            var config = new Mock<IConfigService>();
            var storage = new Mock<IBlobStorageService>();
            var http = new Mock<IHttpClient>();
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            status.Setup(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, StatusModel>());
            config.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetConfig());

            vault.Setup(x => x.GetSecretAsync("Webhook1", It.IsAny<CancellationToken>()))
                .ReturnsAsync("url1");
            vault.Setup(x => x.GetSecretAsync("Webhook2", It.IsAny<CancellationToken>()))
                .ReturnsAsync("url2");

            http.Setup(x => x.PostStreamAsync("url1", It.IsAny<MemoryStream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            http.Setup(x => x.PostAsync("url2", It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var service = new EmailService(status.Object, vault.Object, config.Object, storage.Object, http.Object);

            var success = await service.ProcessMailAsync(request, CancellationToken.None);
            success.Should().BeTrue();

            status.Verify(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "forward-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "notify-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Service_should_call_all_actions_correctly()
        {
            var status = new Mock<IStatusService>();
            var vault = new Mock<IKeyVaultHelper>();
            var config = new Mock<IConfigService>();
            var storage = new Mock<IBlobStorageService>();
            var http = new Mock<IHttpClient>();
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            status.Setup(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, StatusModel>());
            var cfg = GetConfig();
            cfg.Rules[0].Actions = new[]
            {
                new EmailAction
                {
                    Id = "forward-all",
                    Type = ActionType.Forward,
                    Properties = JObject.FromObject(new
                    {
                        webhook = new
                        {
                            secretName = "Webhook1"
                        }
                    })
                },
                new EmailAction
                {
                    Id = "notify-all",
                    Type = ActionType.Webhook,
                    Properties = JObject.FromObject(new
                    {
                        webhook = new
                        {
                            secretName = "Webhook2"
                        },
                        subject = "You've got mail!",
                        body ="%subject%"
                    })
                },
                new EmailAction
                {
                    Id = "archive-all",
                    Type = ActionType.Archive,
                    Properties = JObject.FromObject(new
                    {
                        containerName = "emails"
                    })
                }
            };
            config.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(cfg);

            storage.Setup(x => x.UploadAsync("emails", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            vault.Setup(x => x.GetSecretAsync("Webhook1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync("url1");
            vault.Setup(x => x.GetSecretAsync("Webhook2", It.IsAny<CancellationToken>()))
                    .ReturnsAsync("url2");

            http.Setup(x => x.PostStreamAsync("url1", It.IsAny<MemoryStream>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            http.Setup(x => x.PostAsync("url2", It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var service = new EmailService(status.Object, vault.Object, config.Object, storage.Object, http.Object);

            var success = await service.ProcessMailAsync(request, CancellationToken.None);
            success.Should().BeTrue();

            status.Verify(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "forward-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "notify-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "archive-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.VerifyNoOtherCalls();

            storage.Verify(x => x.UploadAsync("emails", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
            storage.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Service_should_call_all_actions_correctly_with_attachments()
        {
            var status = new Mock<IStatusService>();
            var vault = new Mock<IKeyVaultHelper>();
            var config = new Mock<IConfigService>();
            var storage = new Mock<IBlobStorageService>();
            var http = new Mock<IHttpClient>();
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail-with-attachment.txt"), parser);

            status.Setup(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, StatusModel>());
            var cfg = GetConfig();
            cfg.Rules[0].Actions = new[]
            {
                new EmailAction
                {
                    Id = "forward-all",
                    Type = ActionType.Forward,
                    Properties = JObject.FromObject(new
                    {
                        webhook = new
                        {
                            secretName = "Webhook1"
                        }
                    })
                },
                new EmailAction
                {
                    Id = "notify-all",
                    Type = ActionType.Webhook,
                    Properties = JObject.FromObject(new
                    {
                        webhook = new
                        {
                            secretName = "Webhook2"
                        },
                        subject = "You've got mail!",
                        body ="%subject%"
                    })
                },
                new EmailAction
                {
                    Id = "archive-all",
                    Type = ActionType.Archive,
                    Properties = JObject.FromObject(new
                    {
                        containerName = "emails"
                    })
                }
            };
            config.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(cfg);

            storage.Setup(x => x.UploadAsync("emails", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            storage.Setup(x => x.UploadAsync("emails", It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            vault.Setup(x => x.GetSecretAsync("Webhook1", It.IsAny<CancellationToken>()))
                    .ReturnsAsync("url1");
            vault.Setup(x => x.GetSecretAsync("Webhook2", It.IsAny<CancellationToken>()))
                    .ReturnsAsync("url2");

            http.Setup(x => x.PostStreamAsync("url1", It.IsAny<MemoryStream>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            http.Setup(x => x.PostAsync("url2", It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var service = new EmailService(status.Object, vault.Object, config.Object, storage.Object, http.Object);

            var success = await service.ProcessMailAsync(request, CancellationToken.None);
            success.Should().BeTrue();

            status.Verify(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "forward-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "notify-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "archive-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.VerifyNoOtherCalls();

            storage.Verify(x => x.UploadAsync("emails", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
            storage.Verify(x => x.UploadAsync("emails", It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()));
            storage.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Service_should_invoke_all_actions_even_when_one_fails()
        {
            var status = new Mock<IStatusService>();
            var vault = new Mock<IKeyVaultHelper>();
            var config = new Mock<IConfigService>();
            var storage = new Mock<IBlobStorageService>();
            var http = new Mock<IHttpClient>();
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            status.Setup(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, StatusModel>());
            config.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetConfig());

            vault.Setup(x => x.GetSecretAsync("Webhook1", It.IsAny<CancellationToken>()))
                .Throws(new WebException("pretent secret not found"));

            vault.Setup(x => x.GetSecretAsync("Webhook2", It.IsAny<CancellationToken>()))
                .ReturnsAsync("url2");

            http.Setup(x => x.PostAsync("url2", It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var service = new EmailService(status.Object, vault.Object, config.Object, storage.Object, http.Object);

            try
            {
                await service.ProcessMailAsync(request, CancellationToken.None);
                Assert.Fail();
            }
            catch (AggregateException)
            {
                // expected due to action failure
            }

            status.Verify(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "forward-all"), EmailFanoutStatus.DeferredOrFailed, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "notify-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Multiple_exceptions_should_be_aggregated_when_more_than_one_service_fails()
        {
            var status = new Mock<IStatusService>();
            var vault = new Mock<IKeyVaultHelper>();
            var config = new Mock<IConfigService>();
            var storage = new Mock<IBlobStorageService>();
            var http = new Mock<IHttpClient>();
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            status.Setup(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, StatusModel>());
            config.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetConfig());

            vault.Setup(x => x.GetSecretAsync("Webhook1", It.IsAny<CancellationToken>()))
                .Throws(new WebException("pretent secret not found"));

            vault.Setup(x => x.GetSecretAsync("Webhook2", It.IsAny<CancellationToken>()))
                .ReturnsAsync("url2");

            http.Setup(x => x.PostAsync("url2", It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Throws(new WebException("pretent secret not reachable"));

            var service = new EmailService(status.Object, vault.Object, config.Object, storage.Object, http.Object);

            try
            {
                await service.ProcessMailAsync(request, CancellationToken.None);
                Assert.Fail();
            }
            catch (AggregateException ex)
            {
                // expected due to action failure
                ex.InnerExceptions.Count.Should().Be(2, "because two services failed");
            }

            status.Verify(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "forward-all"), EmailFanoutStatus.DeferredOrFailed, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "notify-all"), EmailFanoutStatus.DeferredOrFailed, It.IsAny<CancellationToken>()));
            status.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Retry_should_only_invoke_non_successful_actions()
        {
            var status = new Mock<IStatusService>();
            var vault = new Mock<IKeyVaultHelper>();
            var config = new Mock<IConfigService>();
            var storage = new Mock<IBlobStorageService>();
            var http = new Mock<IHttpClient>();
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            // pretend email was received before and this is a retry
            status.Setup(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, StatusModel>
                {
                    ["forward-all"] = new StatusModel
                    {
                        ActionId = "forward-all",
                        Status = EmailFanoutStatus.DeferredOrFailed.ToString()
                    },
                    ["notify-all"] = new StatusModel
                    {
                        ActionId = "notify-all",
                        Status = EmailFanoutStatus.Completed.ToString()
                    }
                });
            config.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetConfig());

            vault.Setup(x => x.GetSecretAsync("Webhook1", It.IsAny<CancellationToken>()))
                .ReturnsAsync("url1");

            http.Setup(x => x.PostStreamAsync("url1", It.IsAny<MemoryStream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var service = new EmailService(status.Object, vault.Object, config.Object, storage.Object, http.Object);

            var success = await service.ProcessMailAsync(request, CancellationToken.None);
            success.Should().BeTrue();

            status.Verify(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "forward-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Service_should_ignore_filtered_action()
        {
            var status = new Mock<IStatusService>();
            var vault = new Mock<IKeyVaultHelper>();
            var config = new Mock<IConfigService>();
            var storage = new Mock<IBlobStorageService>();
            var http = new Mock<IHttpClient>();
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            status.Setup(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, StatusModel>());
            var cfg = GetConfig();
            cfg.Rules = new[]
            {
                cfg.Rules[0],
                new EmailRule
                {
                    Filters = new []
                    {
                        new EmailFilter
                        {
                            Type = "subject contains",
                            OneOf = new[]
                            {
                                "not found"
                            }
                        }
                    },
                    Actions = new[]
                    {
                        new EmailAction
                        {
                            Id = "not-executed",
                            Type = ActionType.Forward
                        }
                    }
                }
            };
            config.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cfg);

            vault.Setup(x => x.GetSecretAsync("Webhook1", It.IsAny<CancellationToken>()))
                .ReturnsAsync("url1");
            vault.Setup(x => x.GetSecretAsync("Webhook2", It.IsAny<CancellationToken>()))
                .ReturnsAsync("url2");

            http.Setup(x => x.PostStreamAsync("url1", It.IsAny<MemoryStream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            http.Setup(x => x.PostAsync("url2", It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var service = new EmailService(status.Object, vault.Object, config.Object, storage.Object, http.Object);

            var success = await service.ProcessMailAsync(request, CancellationToken.None);
            success.Should().BeTrue();

            status.Verify(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "forward-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.Verify(x => x.UpdateAsync(request, It.Is<EmailAction>(a => a.Id == "notify-all"), EmailFanoutStatus.Completed, It.IsAny<CancellationToken>()));
            status.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Should_be_successful_even_if_all_filtered()
        {
            var status = new Mock<IStatusService>();
            var vault = new Mock<IKeyVaultHelper>();
            var config = new Mock<IConfigService>();
            var storage = new Mock<IBlobStorageService>();
            var http = new Mock<IHttpClient>();
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            status.Setup(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, StatusModel>());
            var cfg = GetConfig();
            cfg.Rules[0].Filters = new[]
            {
                new EmailFilter
                {
                    Type = "subject contains",
                    OneOf = new[]
                    {
                        "not-found"
                    }
                }
            };
            config.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cfg);

            var service = new EmailService(status.Object, vault.Object, config.Object, storage.Object, http.Object);

            var success = await service.ProcessMailAsync(request, CancellationToken.None);
            success.Should().BeTrue();

            status.Verify(x => x.GetStatiAsync(request, It.IsAny<CancellationToken>()));
            status.VerifyNoOtherCalls();
        }

        [Test]
        public void No_action_filter_should_return_all()
        {
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            var config = GetConfig();

            var actions = EmailService.ActionsToPerform(request.Email, config);
            actions.Select(a => a.Id).Should().ContainInOrder("forward-all", "notify-all");
        }

        [Test]
        public void Unmatched_action_filter_should_ignore_all()
        {
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            var config = GetConfig();
            config.Rules[0].Filters = new[]
            {
                new EmailFilter
                {
                    Type = "subject contains",
                    OneOf = new[]
                    {
                        "not found"
                    }
                }
            };

            var actions = EmailService.ActionsToPerform(request.Email, config);
            actions.Should().BeEmpty();
        }

        [Test]
        public void Subject_filter_should_match()
        {
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            var config = GetConfig();
            config.Rules[0].Filters = new[]
            {
                new EmailFilter
                {
                    Type = "subject contains",
                    OneOf = new[]
                    {
                        "Test 1",
                        "not found"
                    }
                },
                new EmailFilter
                {
                    Type = "sender contains",
                    OneOf = new[]
                    {
                        "not found",
                        "sender@"
                    }
                }
            };

            var actions = EmailService.ActionsToPerform(request.Email, config);
            actions.Select(a => a.Id).Should().ContainInOrder("forward-all", "notify-all");
        }

        [TestCase("sender contains", "sender@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("sender contains", "sender2@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!sender contains", "sender@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!sender contains", "sender2@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("sender equals", "SENDER@example.com", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("sender equals", "sender@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("sender equals", "sender2@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!sender equals", "sender2@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("!sender equals", "SENDER@example.com", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("subject contains", "Test 1", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("subject contains", "Test 2", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!subject contains", "Test 1", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!subject contains", "Test 2", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("body contains", "Test", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("body contains", "Test not", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!body contains", "Test", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!body contains", "Test not", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("subject/body contains", "Test", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("subject/body contains", "Test not", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("subject/body contains", "Test", "sender@example.com", "recipient@example.com", "Subject 1", "Test message", true)]
        [TestCase("subject/body contains", "Test not", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!subject/body contains", "Test", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!subject/body contains", "Test not", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("!subject/body contains", "Test", "sender@example.com", "recipient@example.com", "Subject 1", "Test message", false)]
        [TestCase("!subject/body contains", "Test not", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("recipient contains", "recipient@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("recipient contains", "not@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!recipient contains", "recipient@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!recipient contains", "not@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("recipient equals", "recipient@example.COM", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        [TestCase("recipient equals", "not@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("recipient equals", "not@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!recipient equals", "recipient@example.COM", "sender@example.com", "recipient@example.com", "Test 1", "Test message", false)]
        [TestCase("!recipient equals", "recipient@", "sender@example.com", "recipient@example.com", "Test 1", "Test message", true)]
        public void Subject_filter_should_match_only_its_actions(string rule, string match, string sender, string recipient, string subject, string body, bool shouldMatch)
        {
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);
            request.Email.From.Email = sender;
            request.Email.To[0].Email = recipient;
            request.Email.Subject = subject;
            request.Email.Text = request.Email.Html = body;

            var config = GetConfig();
            config.Rules = new[]
            {
                new EmailRule
                {
                    Filters = new[]
                    {
                        new EmailFilter
                        {
                            Type = rule,
                            OneOf = new[]
                            {
                                match,
                                "not found"
                            }
                        }
                    },
                    Actions = new[]
                    {
                        new EmailAction
                        {
                            Id = "notify-all",
                            Type = ActionType.Webhook,
                            Properties = JObject.FromObject(new
                            {
                                webhook = new
                                {
                                    secretName = "Webhook2"
                                },
                                subject = "You've got mail!",
                                body ="%subject%"
                            })
                        }
                    }
                }
            };

            if (rule.StartsWith("!"))
            {
                config.Rules[0].Filters[0].AllOf = config.Rules[0].Filters[0].OneOf;
                config.Rules[0].Filters[0].OneOf = null;
            }

            var actions = EmailService.ActionsToPerform(request.Email, config);
            if (shouldMatch)
                actions.Select(a => a.Id).Should().ContainInOrder("notify-all");
            else
                actions.Should().BeEmpty();
        }

        [Test]
        public void Multiple_senders_should_not_match()
        {
            var parser = new SendgridEmailParser();
            var request = EmailRequest.Parse(Load("mail.txt"), parser);

            var config = GetConfig();
            config.Rules[0].Filters = new[]
            {
                new EmailFilter
                {
                    Type = "!sender contains",
                    AllOf = new[]
                    {
                        "foo@example.com",
                        "bar@example.com"
                    }
                }
            };

            request.Email.From.Email = "foo@example.com";
            var actions = EmailService.ActionsToPerform(request.Email, config);
            actions.Select(a => a.Id).Should().BeEmpty();

            request.Email.From.Name = request.Email.From.Email = "allowed@example.com";
            actions = EmailService.ActionsToPerform(request.Email, config);
            actions.Select(a => a.Id).Should().ContainInOrder("forward-all", "notify-all");
        }

        private EmailConfig GetConfig()
            => new EmailConfig
            {
                Rules = new[]
                    {
                        new EmailRule
                        {
                            Filters = null,
                            Actions = new[]
                            {
                                new EmailAction
                                {
                                    Id = "forward-all",
                                    Type = ActionType.Forward,
                                    Properties = JObject.FromObject(new
                                    {
                                        webhook = new
                                        {
                                            secretName = "Webhook1"
                                        }
                                    })
                                },
                                new EmailAction
                                {
                                    Id = "notify-all",
                                    Type = ActionType.Webhook,
                                    Properties = JObject.FromObject(new
                                    {
                                        webhook = new
                                        {
                                            secretName = "Webhook2"
                                        },
                                        subject = "You've got mail!",
                                        body ="%subject%"
                                    })
                                }
                            }
                        }
                    }
            };

        private Stream Load(string name)
            => File.OpenRead($"Data/{name}");
    }
}
