using BtmsGateway.Config;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace BtmsGateway.Test.Endpoints;

public class NoConsumersTestBase(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    protected override void ConfigureHostConfiguration(IConfigurationBuilder config)
    {
        base.ConfigureHostConfiguration(config);

        config.AddInMemoryCollection(
            new Dictionary<string, string>
            {
                [$"{nameof(AwsSqsOptions)}:{nameof(AwsSqsOptions.AutoStartConsumers)}"] = "false",
            }
        );
    }
}
