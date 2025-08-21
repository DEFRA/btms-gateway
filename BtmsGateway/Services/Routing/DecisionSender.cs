using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Converter;
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
        : base(apiSender, routingConfig, logger, featureManager)
    {
        _apiSender = apiSender;
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
            "{MessageCorrelationId} {MRN} Sending decision from {MessageSource} to Decision Comparer.",
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
        string? correlationId,
        CancellationToken cancellationToken
    )
    {
        if (messageSource == await MessageSourceToSend())
        {
            _logger.Debug(
                "{MessageCorrelationId} {MRN} Sending Decision received from Decision Comparer to CDS.",
                correlationId,
                mrn
            );

            var comparerDecision = await GetResponseContentAsync(comparerResponse, cancellationToken);

            if (string.IsNullOrWhiteSpace(comparerDecision))
            {
                _logger.Error(
                    "{MessageCorrelationId} {MRN} Decision Comparer returned an invalid decision",
                    correlationId,
                    mrn
                );
                throw new DecisionComparisonException($"{mrn} Decision Comparer returned an invalid decision.");
            }

            var response = await SendCdsFormattedSoapMessageAsync(
                comparerDecision,
                correlationId,
                _btmsToCdsDestination,
                cancellationToken
            );

            var soapContent = new SoapContent(comparerDecision);
            var contentMap = new ContentMap(soapContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error(
                    "{MessageCorrelationId} {MRN} Failed to send Decision to CDS. CDS Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                    contentMap.CorrelationId,
                    mrn,
                    response.StatusCode,
                    response.ReasonPhrase,
                    await GetResponseContentAsync(response, cancellationToken)
                );
                throw new DecisionComparisonException($"{mrn} Failed to send Decision to CDS.");
            }

            _logger.Information(
                "{MessageCorrelationId} {MRN} Successfully sent Decision to CDS.",
                contentMap.CorrelationId,
                mrn
            );

            return response;
        }

        _logger.Information("{MessageCorrelationId} {MRN} Successfully sent Decision to NOOP.", correlationId, mrn);

        return null;
    }
}
