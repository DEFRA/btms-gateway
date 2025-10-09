using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Middleware;
using BtmsGateway.Services.Converter;
using ILogger = Serilog.ILogger;

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

    public DecisionSender(RoutingConfig? routingConfig, IApiSender apiSender, ILogger logger)
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
        var cdsResponse = await ForwardDecisionAsync(mrn, messageSource, decision, correlationId, cancellationToken);

        var destination = string.Concat(_btmsToCdsDestination.Link, _btmsToCdsDestination.RoutePath);

        return routingResult with
        {
            RouteFound = true,
            RouteLinkType = _btmsToCdsDestination.LinkType,
            ForkLinkType = _btmsToCdsDestination.LinkType,
            RoutingSuccessful = true,
            FullRouteLink = destination,
            FullForkLink = destination,
            StatusCode = cdsResponse?.StatusCode ?? HttpStatusCode.NoContent,
            ResponseContent = await GetResponseContentAsync(cdsResponse, cancellationToken),
        };
    }

    private async Task<HttpResponseMessage?> ForwardDecisionAsync(
        string? mrn,
        MessagingConstants.MessageSource messageSource,
        string decision,
        string? correlationId,
        CancellationToken cancellationToken
    )
    {
        if (messageSource == MessagingConstants.MessageSource.Btms)
        {
            _logger.Debug("{MessageCorrelationId} {MRN} Sending Decision to CDS.", correlationId, mrn);

            if (string.IsNullOrWhiteSpace(decision))
            {
                _logger.Error("{MessageCorrelationId} {MRN} Decision invalid", correlationId, mrn);
                throw new DecisionException($"{mrn} Decision invalid.");
            }

            var response = await SendCdsFormattedSoapMessageAsync(
                decision,
                correlationId,
                _btmsToCdsDestination,
                cancellationToken
            );

            var soapContent = new SoapContent(decision);
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
                throw new DecisionException($"{mrn} Failed to send Decision to CDS.");
            }

            _logger.Information(
                "{MessageCorrelationId} {MRN} Successfully sent Decision to CDS.",
                contentMap.CorrelationId,
                mrn
            );

            return response;
        }
        else
        {
            _logger.Information("{MessageCorrelationId} {MRN} Successfully sent Decision to NOOP.", correlationId, mrn);

            throw new DecisionException($"{mrn} Received decision from unexpected source None.");
        }
    }
}
