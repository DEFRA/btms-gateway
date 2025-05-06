using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Utils;
using Microsoft.FeatureManagement;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IDecisionSender
{
    Task<RoutingResult> SendDecisionAsync(
        string? mrn,
        string? decision,
        MessagingConstants.DecisionSource decisionSource,
        IHeaderDictionary? headers = null,
        string? externalCorrelationId = null,
        CancellationToken cancellationToken = default
    );
}

public class DecisionSender : IDecisionSender
{
    private const string CorrelationIdHeaderName = "CorrelationId";
    private const string AcceptHeaderName = "Accept";

    private readonly RoutingConfig? _routingConfig;
    private readonly IApiSender _apiSender;
    private readonly IFeatureManager _featureManager;
    private readonly ILogger _logger;
    private readonly Destination _btmsDecisionsComparerDestination;
    private readonly Destination _alvsDecisionComparerDestination;
    private readonly Destination _btmsToCdsDestination;

    public DecisionSender(
        RoutingConfig? routingConfig,
        IApiSender apiSender,
        IFeatureManager featureManager,
        ILogger logger
    )
    {
        _routingConfig = routingConfig;
        _apiSender = apiSender;
        _featureManager = featureManager;
        _logger = logger;

        _btmsDecisionsComparerDestination = GetDestination(MessagingConstants.Destinations.BtmsDecisionComparer);
        _alvsDecisionComparerDestination = GetDestination(MessagingConstants.Destinations.AlvsDecisionComparer);
        _btmsToCdsDestination = GetDestination(MessagingConstants.Destinations.BtmsCds);
    }

    public async Task<RoutingResult> SendDecisionAsync(
        string? mrn,
        string? decision,
        MessagingConstants.DecisionSource decisionSource,
        IHeaderDictionary? headers = null,
        string? externalCorrelationId = null,
        CancellationToken cancellationToken = default
    )
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
                cancellationToken
            ),
            MessagingConstants.DecisionSource.Alvs => await _apiSender.SendDecisionAsync(
                decision,
                $"{_alvsDecisionComparerDestination.Link}{_alvsDecisionComparerDestination.RoutePath}{mrn}",
                _alvsDecisionComparerDestination.ContentType,
                cancellationToken,
                headers
            ),
            _ => throw new ArgumentException($"{mrn} Received decision from unexpected source {decisionSource}."),
        };

        if (!comparerResponse.StatusCode.IsSuccessStatusCode())
        {
            _logger.Error(
                "{MRN} Failed to send Decision to Decision Comparer: Status Code: {ComparerResponseStatusCode}, Reason: {ComparerResponseReason}.",
                mrn,
                comparerResponse.StatusCode,
                comparerResponse.ReasonPhrase
            );
            throw new DecisionComparisonException($"{mrn} Failed to send Decision to Decision Comparer.");
        }

        var cdsResponse = await ForwardDecisionAsync(
            mrn,
            decisionSource,
            comparerResponse,
            decision,
            externalCorrelationId,
            cancellationToken
        );

        var fullLink =
            decisionSource == MessagingConstants.DecisionSource.Btms
                ? $"{_btmsDecisionsComparerDestination.Link}{_btmsDecisionsComparerDestination.RoutePath}{mrn}"
                : $"{_alvsDecisionComparerDestination.Link}{_alvsDecisionComparerDestination.RoutePath}{mrn}";

        return new RoutingResult
        {
            RouteFound = true,
            RouteLinkType = LinkType.DecisionComparer,
            ForkLinkType = LinkType.DecisionComparer,
            RoutingSuccessful = true,
            FullRouteLink = fullLink,
            FullForkLink = fullLink,
            StatusCode = cdsResponse?.StatusCode ?? HttpStatusCode.NoContent,
            ResponseContent = await GetResponseContentAsync(cdsResponse, cancellationToken),
        };
    }

    private async Task<HttpResponseMessage?> ForwardDecisionAsync(
        string? mrn,
        MessagingConstants.DecisionSource decisionSource,
        HttpResponseMessage? comparerResponse,
        string originalDecision,
        string? externalCorrelationId,
        CancellationToken cancellationToken
    )
    {
        if (
            await _featureManager.IsEnabledAsync(Features.SendOnlyBtmsDecisionToCds)
            && decisionSource == MessagingConstants.DecisionSource.Btms
        )
        {
            _logger.Debug("{MRN} Sending BTMS Decision to CDS.", mrn);

            return await SendCdsFormattedSoapMessageAsync(
                mrn,
                originalDecision,
                externalCorrelationId,
                cancellationToken
            );
        }

        if (
            !await _featureManager.IsEnabledAsync(Features.SendOnlyBtmsDecisionToCds)
            && decisionSource == MessagingConstants.DecisionSource.Alvs
        )
        {
            _logger.Debug("{MRN} Sending Decision received from Decision Comparer to CDS.", mrn);

            var comparerDecision = await GetResponseContentAsync(comparerResponse, cancellationToken);

            if (string.IsNullOrWhiteSpace(comparerDecision))
            {
                _logger.Error(
                    "{MRN} Decision Comparer returned an invalid decision: {ComparerDecision}.",
                    mrn,
                    comparerDecision
                );
                throw new DecisionComparisonException($"{mrn} Decision Comparer returned an invalid decision.");
            }

            _logger.Information(
                "{MRN} Received Decision from Decision Comparer: {ComparerDecision}",
                mrn,
                comparerDecision
            );
            // Just log decision for now. Eventually, in cut over, will send the Decision to CDS.
            // Ensure original ALVS request headers are passed through and appended in SendCdsFormattedSoapMessageAsync!
            // Pass the CDS response back out!
            return null;
        }

        return null;
    }

    private async Task<HttpResponseMessage?> SendCdsFormattedSoapMessageAsync(
        string? mrn,
        string soapMessage,
        string? externalCorrelationId,
        CancellationToken cancellationToken
    )
    {
        var destination = string.Concat(_btmsToCdsDestination.Link, _btmsToCdsDestination.RoutePath);
        var headers = new Dictionary<string, string> { { AcceptHeaderName, _btmsToCdsDestination.ContentType } };

        if (!string.IsNullOrWhiteSpace(externalCorrelationId))
            headers.Add(CorrelationIdHeaderName, externalCorrelationId);

        var response = await _apiSender.SendSoapMessageAsync(
            _btmsToCdsDestination.Method ?? "POST",
            destination,
            _btmsToCdsDestination.ContentType,
            _btmsToCdsDestination.HostHeader,
            headers,
            soapMessage,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            _logger.Error(
                "{MRN} Failed to send clearance decision to CDS. CDS Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                mrn,
                response.StatusCode,
                response.ReasonPhrase,
                await GetResponseContentAsync(response, cancellationToken)
            );
            throw new DecisionComparisonException($"{mrn} Failed to send clearance decision to CDS.");
        }

        return response;
    }

    private Destination GetDestination(string destinationKey)
    {
        Destination? destination = null;

        if (
            (!_routingConfig?.Destinations.TryGetValue(destinationKey, out destination) ?? false) || destination is null
        )
        {
            _logger.Error(
                "Destination configuration could not be found for {DestinationKey}. Please confirm application configuration contains the Destination configuration.",
                destinationKey
            );
            throw new ArgumentException($"Destination configuration could not be found for {destinationKey}.");
        }

        return destination;
    }

    private static async Task<string> GetResponseContentAsync(
        HttpResponseMessage? response,
        CancellationToken cancellationToken
    )
    {
        if (response is not null && response.StatusCode != HttpStatusCode.NoContent)
            return await response.Content.ReadAsStringAsync(cancellationToken);

        return string.Empty;
    }
}
