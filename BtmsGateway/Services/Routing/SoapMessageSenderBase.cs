using System.Net;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public abstract class SoapMessageSenderBase(IApiSender apiSender, RoutingConfig? routingConfig, ILogger logger)
{
    private const string CorrelationIdHeaderName = "CorrelationId";
    private const string AcceptHeaderName = "Accept";

    protected Destination GetDestination(string destinationKey)
    {
        Destination? destination = null;

        if ((!routingConfig?.Destinations.TryGetValue(destinationKey, out destination) ?? false) || destination is null)
        {
            logger.Error(
                "Destination configuration could not be found for {DestinationKey}. Please confirm application configuration contains the Destination configuration.",
                destinationKey
            );
            throw new ArgumentException($"Destination configuration could not be found for {destinationKey}.");
        }

        return destination;
    }

    protected async Task<HttpResponseMessage> SendCdsFormattedSoapMessageAsync(
        string soapMessage,
        string? correlationId,
        Destination btmsToCdsDestination,
        CancellationToken cancellationToken
    )
    {
        var destination = string.Concat(btmsToCdsDestination.Link, btmsToCdsDestination.RoutePath);
        var headers = new Dictionary<string, string> { { AcceptHeaderName, btmsToCdsDestination.ContentType } };

        if (!string.IsNullOrWhiteSpace(correlationId))
            headers.Add(CorrelationIdHeaderName, correlationId);

        return await apiSender.SendSoapMessageAsync(
            btmsToCdsDestination.Method ?? "POST",
            destination,
            btmsToCdsDestination.ContentType,
            btmsToCdsDestination.HostHeader,
            headers,
            soapMessage,
            cancellationToken
        );
    }

    protected static (string DestinationUrl, string ContentType) GetDestinationConfiguration(
        string? mrn,
        Destination destination
    )
    {
        var destinationUrl = $"{destination.Link}{destination.RoutePath}{mrn}";
        var contentType = destination.ContentType;

        return (destinationUrl, contentType);
    }

    protected static async Task<string> GetResponseContentAsync(
        HttpResponseMessage? response,
        CancellationToken cancellationToken
    )
    {
        if (response is not null && response.StatusCode != HttpStatusCode.NoContent)
            return await response.Content.ReadAsStringAsync(cancellationToken);

        return string.Empty;
    }
}
