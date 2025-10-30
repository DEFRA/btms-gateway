using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Converter;

namespace BtmsGateway.Services.Routing;

public interface IDecisionSender
{
    Task<RoutingResult> SendDecisionAsync(
        string? mrn,
        string decision,
        MessagingConstants.MessageSource messageSource,
        RoutingResult routingResult,
        IHeaderDictionary? headers = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    );
}

public class DecisionSender : SoapMessageSenderBase, IDecisionSender
{
    private readonly ILogger _logger;
    private readonly Destination _btmsToCdsDestination;

    public DecisionSender(RoutingConfig? routingConfig, IApiSender apiSender, ILogger<DecisionSender> logger)
        : base(apiSender, routingConfig, logger)
    {
        _logger = logger;

        _btmsToCdsDestination = GetDestination(MessagingConstants.Destinations.BtmsCds);
    }

    public async Task<RoutingResult> SendDecisionAsync(
        string? mrn,
        string decision,
        MessagingConstants.MessageSource messageSource,
        RoutingResult routingResult,
        IHeaderDictionary? headers = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    )
    {
        if (messageSource != MessagingConstants.MessageSource.Btms)
        {
            throw new CdsCommunicationException($"{mrn} Received decision from unexpected source None.");
        }

        if (string.IsNullOrWhiteSpace(decision))
        {
            _logger.LogError("{MessageCorrelationId} {MRN} Decision invalid", correlationId, mrn);
            throw new CdsCommunicationException($"{mrn} Decision invalid.");
        }

        _logger.LogDebug("{MessageCorrelationId} {MRN} Sending Decision to CDS.", correlationId, mrn);

        var cdsResponse = await SendCdsFormattedSoapMessageAsync(
            decision,
            correlationId,
            _btmsToCdsDestination,
            cancellationToken
        );

        var soapContent = new SoapContent(decision);
        var contentMap = new ContentMap(soapContent);

        if (!cdsResponse.IsSuccessStatusCode)
        {
            _logger.LogError(
                "{MessageCorrelationId} {MRN} Failed to send Decision to CDS. CDS Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                contentMap.CorrelationId,
                mrn,
                cdsResponse.StatusCode,
                cdsResponse.ReasonPhrase,
                await GetResponseContentAsync(cdsResponse, cancellationToken)
            );
            throw new CdsCommunicationException($"{mrn} Failed to send Decision to CDS.");
        }

        _logger.LogInformation(
            "{MessageCorrelationId} {MRN} Successfully sent Decision to CDS.",
            contentMap.CorrelationId,
            mrn
        );

        var destination = string.Concat(_btmsToCdsDestination.Link, _btmsToCdsDestination.RoutePath);

        return routingResult with
        {
            RouteFound = true,
            RouteLinkType = _btmsToCdsDestination.LinkType,
            RoutingSuccessful = true,
            FullRouteLink = destination,
            StatusCode = cdsResponse.StatusCode,
            ResponseContent = await GetResponseContentAsync(cdsResponse, cancellationToken),
        };
    }
}
