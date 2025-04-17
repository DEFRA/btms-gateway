using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using SlimMessageBus.Host.AmazonSQS;

namespace BtmsGateway.Config;

public sealed class CdpCredentialsSqsClientProvider : ISqsClientProvider, IDisposable
{
    private const string DefaultRegion = "eu-west-2";
    private bool _disposedValue;

    public CdpCredentialsSqsClientProvider(AmazonSQSConfig sqsConfig, IConfiguration configuration)
    {
        var clientId = configuration.GetValue<string>("AWS_ACCESS_KEY_ID");
        var clientSecret = configuration.GetValue<string>("AWS_SECRET_ACCESS_KEY");

        if (!string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(clientId))
        {
            var region = configuration.GetValue<string>("AWS_REGION") ?? DefaultRegion;
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);

            Client = new AmazonSQSClient(
                new BasicAWSCredentials(clientId, clientSecret),
                new AmazonSQSConfig
                {
                    AuthenticationRegion = region,
                    RegionEndpoint = regionEndpoint,
                    ServiceURL = configuration.GetValue<string>("SQS_Endpoint"),
                }
            );

            return;
        }

        Client = new AmazonSQSClient(sqsConfig);
    }

    #region ISqsClientProvider

    public AmazonSQSClient Client { get; }

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
        GC.SuppressFinalize(this);
    }

    #endregion
}