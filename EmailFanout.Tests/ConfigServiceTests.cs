using EmailFanout.Logic;
using EmailFanout.Logic.Models;
using EmailFanout.Logic.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmailFanout.Tests
{
    public class ConfigServiceTests
    {
        [Test]
        public async Task Valid_config_should_load()
        {
            const string containerName = "cfg";
            var blob = new Mock<IBlobStorageService>();
            var json = File.ReadAllText("Data/config.json");
            blob.Setup(x => x.DownloadAsync(containerName, "email-fanout.json", It.IsAny<CancellationToken>()))
                .ReturnsAsync(json);
            var configuration = new Mock<IConfiguration>();
            configuration.SetupGet(x => x["ConfigContainerName"])
                .Returns(containerName);
            IConfigService service = new ConfigService(blob.Object, configuration.Object);

            var config = await service.LoadAsync(CancellationToken.None);
            config.Rules.Should().HaveCount(1);
            config.Rules[0].Enabled.Should().BeTrue();
            config.Rules[0].Filters.Should().HaveCount(1);
            config.Rules[0].Filters[0].Type.Should().Be("sender contains");
            config.Rules[0].Filters[0].OneOf.Should().HaveCount(1);

            config.Rules[0].Actions.Should().HaveCount(2);
            config.Rules[0].Actions[0].Enabled.Should().BeTrue();
            config.Rules[0].Actions[0].Id.Should().Be("archive-all");
            config.Rules[0].Actions[0].Type.Should().Be(ActionType.Archive);
            config.Rules[0].Actions[1].Id.Should().Be("forward-all");
            config.Rules[0].Actions[1].Type.Should().Be(ActionType.Forward);
        }

        [Test]
        public async Task Valid_config_with_disabled_should_load()
        {
            const string containerName = "cfg";
            var blob = new Mock<IBlobStorageService>();
            var json = File.ReadAllText("Data/config-disabled.json");
            blob.Setup(x => x.DownloadAsync(containerName, "email-fanout.json", It.IsAny<CancellationToken>()))
                .ReturnsAsync(json);
            var configuration = new Mock<IConfiguration>();
            configuration.SetupGet(x => x["ConfigContainerName"])
                .Returns(containerName);
            IConfigService service = new ConfigService(blob.Object, configuration.Object);

            var config = await service.LoadAsync(CancellationToken.None);
            config.Rules.Should().HaveCount(2);
            config.Rules[0].Enabled.Should().BeFalse();
            config.Rules[1].Enabled.Should().BeTrue();
            config.Rules[1].Filters.Should().HaveCount(2);
            config.Rules[1].Filters[0].Enabled.Should().BeTrue();
            config.Rules[1].Filters[0].Type.Should().Be("sender contains");
            config.Rules[1].Filters[0].OneOf.Should().HaveCount(1);
            config.Rules[1].Filters[1].Enabled.Should().BeFalse();
            config.Rules[1].Filters[1].Type.Should().Be("!sender contains");
            config.Rules[1].Filters[1].OneOf.Should().HaveCount(1);

            config.Rules[1].Actions.Should().HaveCount(2);
            config.Rules[1].Actions[0].Enabled.Should().BeFalse();
            config.Rules[1].Actions[1].Enabled.Should().BeTrue();
            config.Rules[1].Actions[1].Id.Should().Be("forward-all");
            config.Rules[1].Actions[1].Type.Should().Be(ActionType.Forward);
        }

        [Test]
        public async Task Duplicate_ids_should_throw()
        {
            const string containerName = "cfg";
            var blob = new Mock<IBlobStorageService>();
            var json = File.ReadAllText("Data/config.json");
            json = json.Replace("\"archive-all\"", "\"forward-all\"");
            blob.Setup(x => x.DownloadAsync(containerName, "email-fanout.json", It.IsAny<CancellationToken>()))
                .ReturnsAsync(json);
            var configuration = new Mock<IConfiguration>();
            configuration.SetupGet(x => x["ConfigContainerName"])
                .Returns(containerName);
            IConfigService service = new ConfigService(blob.Object, configuration.Object);

            try
            {
                await service.LoadAsync(CancellationToken.None);
                Assert.Fail();
            }
            catch (ConfigurationException)
            {
            }
        }
    }
}
