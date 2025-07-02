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
        MessagingConstants.MessageSource messageSource,
        RoutingResult routingResult,
        IHeaderDictionary? headers = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    );
}

public class DecisionSender : SoapMessageSenderBase, IDecisionSender
{
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
        : base(apiSender, routingConfig, logger)
    {
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
        MessagingConstants.MessageSource messageSource,
        RoutingResult routingResult,
        IHeaderDictionary? headers = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    )
    {
        _logger.Debug(
            "{CorrelationId} {MRN} Sending decision from {MessageSource} to Decision Comparer.",
            correlationId,
            mrn,
            messageSource
        );

        if (string.IsNullOrWhiteSpace(decision))
            throw new ArgumentException($"{mrn} Request to send an invalid decision to Decision Comparer: {decision}");

        var destinationConfig = messageSource switch
        {
            MessagingConstants.MessageSource.Btms => GetDestinationConfiguration(
                mrn,
                _btmsDecisionsComparerDestination
            ),
            MessagingConstants.MessageSource.Alvs => GetDestinationConfiguration(mrn, _alvsDecisionComparerDestination),
            _ => throw new ArgumentException($"{mrn} Received decision from unexpected source {messageSource}."),
        };

        var comparerResponse = await _apiSender.SendToDecisionComparerAsync(
            decision,
            destinationConfig.DestinationUrl,
            destinationConfig.ContentType,
            cancellationToken,
            headers
        );

        CheckComparerResponse(comparerResponse, correlationId, mrn, "Decision");

        var cdsResponse = await ForwardDecisionAsync(
            mrn,
            messageSource,
            comparerResponse,
            decision,
            correlationId,
            cancellationToken
        );

        return routingResult with
        {
            RouteFound = true,
            RouteLinkType = LinkType.DecisionComparer,
            ForkLinkType = LinkType.DecisionComparer,
            RoutingSuccessful = true,
            FullRouteLink = destinationConfig.DestinationUrl,
            FullForkLink = destinationConfig.DestinationUrl,
            StatusCode = cdsResponse?.StatusCode ?? HttpStatusCode.NoContent,
            ResponseContent = await GetResponseContentAsync(cdsResponse, cancellationToken),
        };
    }

    private async Task<HttpResponseMessage?> ForwardDecisionAsync(
        string? mrn,
        MessagingConstants.MessageSource messageSource,
        HttpResponseMessage? comparerResponse,
        string originalDecision,
        string? correlationId,
        CancellationToken cancellationToken
    )
    {
        if (
            await _featureManager.IsEnabledAsync(Features.SendOnlyBtmsDecisionToCds)
            && messageSource == MessagingConstants.MessageSource.Btms
        )
        {
            _logger.Debug("{CorrelationId} {MRN} Sending BTMS Decision to CDS.", correlationId, mrn);

            var response = await SendCdsFormattedSoapMessageAsync(
                originalDecision,
                correlationId,
                _btmsToCdsDestination,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error(
                    "{CorrelationId} {MRN} Failed to send clearance decision to CDS. CDS Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                    correlationId,
                    mrn,
                    response.StatusCode,
                    response.ReasonPhrase,
                    await GetResponseContentAsync(response, cancellationToken)
                );
                throw new DecisionComparisonException($"{mrn} Failed to send clearance decision to CDS.");
            }

            return response;
        }

        if (
            !await _featureManager.IsEnabledAsync(Features.SendOnlyBtmsDecisionToCds)
            && messageSource == MessagingConstants.MessageSource.Alvs
        )
        {
            _logger.Debug(
                "{CorrelationId} {MRN} Sending Decision received from Decision Comparer to CDS.",
                correlationId,
                mrn
            );

            var comparerDecision = await GetResponseContentAsync(comparerResponse, cancellationToken);

            if (string.IsNullOrWhiteSpace(comparerDecision))
            {
                _logger.Error(
                    "{CorrelationId} {MRN} Decision Comparer returned an invalid decision",
                    correlationId,
                    mrn
                );
                throw new DecisionComparisonException($"{mrn} Decision Comparer returned an invalid decision.");
            }

            _logger.Information(
                "{CorrelationId} {MRN} Received Decision from Decision Comparer to send to CDS",
                correlationId,
                mrn
            );
            // Just log decision for now. Eventually, in cut over, will send the Decision to CDS.
            // Ensure original ALVS request headers are passed through and appended in SendCdsFormattedSoapMessageAsync!
            // Pass the CDS response back out!
            return null;
        }

        return null;
    }
}
