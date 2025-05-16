using BtmsGateway.Domain;
using BtmsGateway.Services.Metrics;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;

namespace BtmsGateway.Test.Services.Metrics;

public class InstanceMetadataTests
{
    [Fact]
    public async Task When_init_Then_should_set_instance_id()
    {
        var logger = Substitute.For<ILogger<InstanceMetadataTests>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        var apiSender = Substitute.For<IApiSender>();
        apiSender
            .GetEcsMetadataAsync(Arg.Any<CancellationToken>())
            .Returns(new EcsMetadata { TaskArn = "aws_account_arn/TestId" });

        await InstanceMetadata.InitAsync(apiSender, loggerFactory);

        InstanceMetadata.InstanceId.Should().Be("TestId");
    }

    [Fact]
    public async Task When_init_and_ecs_metadata_is_null_Then_should_set_instance_id_to_guid()
    {
        var logger = Substitute.For<ILogger<InstanceMetadataTests>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        var apiSender = Substitute.For<IApiSender>();
        apiSender.GetEcsMetadataAsync(Arg.Any<CancellationToken>()).ReturnsNull();

        await InstanceMetadata.InitAsync(apiSender, loggerFactory);

        InstanceMetadata.InstanceId.Should().NotBeNullOrEmpty();
        Guid.TryParse(InstanceMetadata.InstanceId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task When_init_and_exception_occurs_Then_should_set_instance_id_to_guid()
    {
        var logger = Substitute.For<ILogger<InstanceMetadataTests>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        var apiSender = Substitute.For<IApiSender>();
        apiSender.GetEcsMetadataAsync(Arg.Any<CancellationToken>()).ThrowsAsync<Exception>();

        await InstanceMetadata.InitAsync(apiSender, loggerFactory);

        InstanceMetadata.InstanceId.Should().NotBeNullOrEmpty();
        Guid.TryParse(InstanceMetadata.InstanceId, out _).Should().BeTrue();
    }
}
