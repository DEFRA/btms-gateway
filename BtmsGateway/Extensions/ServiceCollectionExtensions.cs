using System.Net;
using System.Net.Http.Headers;
using BtmsGateway.Config;
using Defra.TradeImportsDataApi.Api.Client;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using SlimMessageBus.Host;

namespace BtmsGateway.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsumers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSlimMessageBus(messageBusBuilder =>
        {
            var awsSqsOptions = services.AddValidateOptions<AwsSqsOptions>(configuration, AwsSqsOptions.SectionName).Get();
            messageBusBuilder.AddAmazonConsumers(awsSqsOptions, configuration);
        });

        return services;
    }

    public static IServiceCollection AddDataApiHttpClient(this IServiceCollection services)
    {
        services.AddOptions<DataApiOptions>().BindConfiguration(DataApiOptions.SectionName).ValidateDataAnnotations();

        services
            .AddTradeImportsDataApiClient()
            .ConfigureHttpClient(
                (sp, c) =>
                {
                    var options = sp.GetRequiredService<IOptions<DataApiOptions>>().Value;
                    c.BaseAddress = new Uri(options.BaseAddress);

                    if (options.BasicAuthCredential != null)
                        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                            "Basic",
                            options.BasicAuthCredential
                        );

                    if (c.BaseAddress.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                        c.DefaultRequestVersion = HttpVersion.Version20;
                }
            )
            .AddStandardResilienceHandler(o =>
            {
                o.Retry.DisableForUnsafeHttpMethods();
            });

        return services;
    }
}