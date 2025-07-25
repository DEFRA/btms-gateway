using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Utils;
using Microsoft.FeatureManagement;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public abstract class SoapMessageSenderBase(
    IApiSender apiSender,
    RoutingConfig? routingConfig,
    ILogger logger,
    IFeatureManager featureManager
)
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

    protected void CheckComparerResponse(
        HttpResponseMessage comparerResponse,
        string? correlationId,
        string? mrn,
        string messageType
    )
    {
        if (comparerResponse.StatusCode == HttpStatusCode.Conflict)
        {
            logger.Warning(
                "{CorrelationId} {MRN} Failed to send {MessageType} to Decision Comparer: Status Code: {ComparerResponseStatusCode}, Reason: {ComparerResponseReason}.",
                correlationId,
                mrn,
                messageType,
                comparerResponse.StatusCode,
                comparerResponse.ReasonPhrase
            );
            throw new ConflictException($"{mrn} Failed to send {messageType} to Decision Comparer.");
        }

        if (!comparerResponse.StatusCode.IsSuccessStatusCode())
        {
            logger.Error(
                "{CorrelationId} {MRN} Failed to send {MessageType} to Decision Comparer: Status Code: {ComparerResponseStatusCode}, Reason: {ComparerResponseReason}.",
                correlationId,
                mrn,
                messageType,
                comparerResponse.StatusCode,
                comparerResponse.ReasonPhrase
            );
            throw new DecisionComparisonException($"{mrn} Failed to send {messageType} to Decision Comparer.");
        }
    }

    protected async Task<MessagingConstants.MessageSource> MessageSourceToSend()
    {
        if (
            await featureManager.IsEnabledAsync(Features.TrialCutover)
            && !await featureManager.IsEnabledAsync(Features.Cutover)
        )
            return MessagingConstants.MessageSource.Alvs; // Only send Decision/Error Notification if it was triggered by an ALVS request during Trial Cutover

        if (await featureManager.IsEnabledAsync(Features.Cutover))
            return MessagingConstants.MessageSource.Btms; // Only send Decision/Error Notification if it was triggered by a BTMS Decision event during Cutover

        return MessagingConstants.MessageSource.None; // Don't send any Decision/Error Notification during Connected Silent Running
    }
}
