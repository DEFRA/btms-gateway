using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IErrorNotificationSender
{
    Task<RoutingResult> SendErrorNotificationAsync(
        string? mrn,
        string? errorNotification,
        MessagingConstants.MessageSource messageSource,
        RoutingResult routingResult,
        IHeaderDictionary? headers = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    );
}

public class ErrorNotificationSender : SoapMessageSenderBase, IErrorNotificationSender
{
    private readonly Destination _btmsToCdsDestination;
    private readonly ILogger _logger;

    public ErrorNotificationSender(RoutingConfig? routingConfig, IApiSender apiSender, ILogger logger)
        : base(apiSender, routingConfig, logger)
    {
        _logger = logger;

        _btmsToCdsDestination = GetDestination(MessagingConstants.Destinations.BtmsCds);
    }

    public async Task<RoutingResult> SendErrorNotificationAsync(
        string? mrn,
        string? errorNotification,
        MessagingConstants.MessageSource messageSource,
        RoutingResult routingResult,
        IHeaderDictionary? headers = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    )
    {
        if (messageSource != MessagingConstants.MessageSource.Btms)
        {
            throw new CdsCommunicationException($"{mrn} Received error notification from unexpected source None.");
        }

        if (string.IsNullOrWhiteSpace(errorNotification))
            throw new CdsCommunicationException(
                $"{mrn} Request to send an invalid error notification to CDS: {errorNotification}"
            );

        _logger.Debug(
            "{MessageCorrelationId} {MRN} Sending error notification from {MessageSource} to CDS.",
            correlationId,
            mrn,
            messageSource
        );

        var cdsResponse = await SendCdsFormattedSoapMessageAsync(
            errorNotification,
            correlationId,
            _btmsToCdsDestination,
            cancellationToken
        );

        if (!cdsResponse.IsSuccessStatusCode)
        {
            _logger.Error(
                "{MessageCorrelationId} {MRN} Failed to send error notification to CDS. CDS Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                correlationId,
                mrn,
                cdsResponse.StatusCode,
                cdsResponse.ReasonPhrase,
                await GetResponseContentAsync(cdsResponse, cancellationToken)
            );
            throw new CdsCommunicationException($"{mrn} Failed to send error notification to CDS.");
        }

        _logger.Information(
            "{MessageCorrelationId} {MRN} Successfully sent {MessageSource} Error Notification to CDS.",
            correlationId,
            mrn,
            messageSource
        );

        var destination = string.Concat(_btmsToCdsDestination.Link, _btmsToCdsDestination.RoutePath);
        return routingResult with
        {
            RouteFound = true,
            RouteLinkType = _btmsToCdsDestination.LinkType,
            ForkLinkType = _btmsToCdsDestination.LinkType,
            RoutingSuccessful = true,
            FullRouteLink = destination,
            FullForkLink = destination,
            StatusCode = cdsResponse.StatusCode,
            ResponseContent = await GetResponseContentAsync(cdsResponse, cancellationToken),
        };
    }
}
