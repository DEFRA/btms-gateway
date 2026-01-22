using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using BtmsGateway.Extensions;
using SlimMessageBus.Host.AmazonSQS;

namespace BtmsGateway.Config;

public sealed class CdpCredentialsSnsClientProvider : ISnsClientProvider, IDisposable
{
    private const string DefaultRegion = "eu-west-2";
    private bool _disposedValue;

    public CdpCredentialsSnsClientProvider(
        AmazonSimpleNotificationServiceConfig sqsConfig,
        IConfiguration configuration
    )
    {
        var clientId = configuration.GetValue<string>("AWS_ACCESS_KEY_ID");
        var clientSecret = configuration.GetValue<string>("AWS_SECRET_ACCESS_KEY");

        if (!string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(clientId))
        {
            var region = configuration.GetValue<string>("AWS_REGION") ?? DefaultRegion;
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);

            Client = new BtmsAmazonSimpleNotificationService(
                new BasicAWSCredentials(clientId, clientSecret),
                new AmazonSimpleNotificationServiceConfig
                {
                    AuthenticationRegion = region,
                    RegionEndpoint = regionEndpoint,
                    ServiceURL = configuration.GetValue<string>("SQS_Endpoint"),
                },
                configuration
            );

            return;
        }

        Client = new BtmsAmazonSimpleNotificationService(sqsConfig, configuration);
    }

    #region ISqsClientProvider

    public IAmazonSimpleNotificationService? Client { get; }

    public Task EnsureClientAuthenticated()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Dispose Pattern

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;

        if (disposing)
            Client?.Dispose();

        _disposedValue = true;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
    }

    #endregion
}
