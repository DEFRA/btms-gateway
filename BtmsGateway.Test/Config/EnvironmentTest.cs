using BtmsGateway.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace BtmsGateway.Test.Config;

public class EnvironmentTest
{
    [Fact]
    public void When_not_in_production_environment_then_web_application_builder_is_dev_mode()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Development }
        );

        var isDev = BtmsGateway.Config.Environment.IsDevMode(builder);

        Assert.True(isDev);
    }

    [Fact]
    public void When_in_production_environment_then_web_application_builder_is_not_in_dev_mode()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Production }
        );

        var isDev = BtmsGateway.Config.Environment.IsDevMode(builder);

        Assert.False(isDev);
    }

    [Fact]
    public void When_not_in_production_environment_then_web_application_is_dev_mode()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Development }
        );
        var app = builder.Build();

        var isDev = app.IsDevMode();

        Assert.True(isDev);
    }

    [Fact]
    public void When_in_production_environment_then_web_application_is_not_in_dev_mode()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Production }
        );
        var app = builder.Build();

        var isDev = app.IsDevMode();

        Assert.False(isDev);
    }
}
