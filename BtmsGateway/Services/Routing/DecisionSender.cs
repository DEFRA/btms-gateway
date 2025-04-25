using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Utils;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IDecisionSender
{
    Task<RoutingResult> SendDecisionAsync(
        string? mrn,
        string? decision,
        MessagingConstants.DecisionSource decisionSource,
        IHeaderDictionary? headers = null,
        CancellationToken cancellationToken = default);
}

public class DecisionSender : IDecisionSender
{
    private readonly RoutingConfig? _routingConfig;
    private readonly IApiSender _apiSender;
    private readonly ILogger _logger;
    private readonly Destination _btmsDecisionsComparerDestination;
    private readonly Destination _alvsDecisionComparerDestination;

    public DecisionSender(RoutingConfig? routingConfig, IApiSender apiSender, ILogger logger)
    {
        _routingConfig = routingConfig;
        _apiSender = apiSender;
        _logger = logger;

        _btmsDecisionsComparerDestination = GetDestination(MessagingConstants.Destinations.BtmsDecisionComparer);
        _alvsDecisionComparerDestination = GetDestination(MessagingConstants.Destinations.AlvsDecisionComparer);
    }

    public async Task<RoutingResult> SendDecisionAsync(
        string? mrn,
        string? decision,
        MessagingConstants.DecisionSource decisionSource,
        IHeaderDictionary? headers = null,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("{MRN} Sending decision from {DecisionSource} to Decision Comparer.", mrn, decisionSource);

        if (string.IsNullOrWhiteSpace(decision))
            throw new ArgumentException($"{mrn} Request to send an invalid decision to Decision Comparer: {decision}");

        var comparerResponse = decisionSource switch
        {
            MessagingConstants.DecisionSource.Btms => await _apiSender.SendDecisionAsync(
                decision,
                $"{_btmsDecisionsComparerDestination.Link}{_btmsDecisionsComparerDestination.RoutePath}{mrn}",
                _btmsDecisionsComparerDestination.ContentType,
                cancellationToken),
            MessagingConstants.DecisionSource.Alvs => await _apiSender.SendDecisionAsync(
                decision,
                $"{_alvsDecisionComparerDestination.Link}{_alvsDecisionComparerDestination.RoutePath}{mrn}",
                _alvsDecisionComparerDestination.ContentType,
                cancellationToken,
                headers),
            _ => throw new ArgumentException($"{mrn} Received decision from unexpected source {decisionSource}.")
        };

        if (!comparerResponse.StatusCode.IsSuccessStatusCode())
        {
            _logger.Error("{MRN} Failed to send Decision to Decision Comparer: Status Code: {ComparerResponseStatusCode}, Reason: {ComparerResponseReason}.",
                mrn,
                comparerResponse.StatusCode,
                comparerResponse.ReasonPhrase);
            throw new DecisionComparisonException($"{mrn} Failed to send Decision to Decision Comparer.");
        }

        await ForwardDecisionAsync(mrn, decisionSource, comparerResponse, cancellationToken);

        return new RoutingResult
        {
            RouteFound = true,
            RouteLinkType = LinkType.DecisionComparer,
            RoutingSuccessful = true,
            FullRouteLink = decisionSource == MessagingConstants.DecisionSource.Btms ?
                $"{_btmsDecisionsComparerDestination.Link}{_btmsDecisionsComparerDestination.RoutePath}{mrn}" :
                $"{_alvsDecisionComparerDestination.Link}{_alvsDecisionComparerDestination.RoutePath}{mrn}",
            StatusCode = comparerResponse.StatusCode,
            ResponseContent = "Decision Comparer Result"
        };
    }

    private async Task ForwardDecisionAsync(string? mrn,
        MessagingConstants.DecisionSource decisionSource,
        HttpResponseMessage? comparerResponse,
        CancellationToken cancellationToken)
    {
        if (decisionSource == MessagingConstants.DecisionSource.Alvs)
        {
            var comparerDecision = await GetResponseContentAsync(comparerResponse, cancellationToken);

            if (string.IsNullOrWhiteSpace(comparerDecision))
            {
                _logger.Error("{MRN} Decision Comparer returned an invalid decision: {ComparerDecision}.",
                    mrn,
                    comparerDecision);
                throw new DecisionComparisonException($"{mrn} Decision Comparer returned an invalid decision.");
            }

            _logger.Information("{MRN} Received Decision from Decision Comparer: {ComparerDecision}", mrn, comparerDecision);
            // Just log decision for now. Eventually, in cut over, will send the Decision to CDS.
        }
    }

    private Destination GetDestination(string destinationKey)
    {
        Destination? destination = null;

        if ((!_routingConfig?.Destinations.TryGetValue(destinationKey, out destination) ?? false) || destination is null)
        {
            _logger.Error("Destination configuration could not be found for {DestinationKey}. Please confirm application configuration contains the Destination configuration.",
                destinationKey);
            throw new ArgumentException($"Destination configuration could not be found for {destinationKey}.");
        }

        return destination;
    }

    private static async Task<string> GetResponseContentAsync(HttpResponseMessage? response, CancellationToken cancellationToken)
    {
        if (response is not null && response.StatusCode != HttpStatusCode.NoContent)
            return await response.Content.ReadAsStringAsync(cancellationToken);

        return string.Empty;
    }
}