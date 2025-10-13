using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;

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

    public ErrorNotificationSender(
        RoutingConfig? routingConfig,
        IApiSender apiSender,
        ILogger<ErrorNotificationSender> logger
    )
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
        _logger.LogDebug(
            "{MessageCorrelationId} {MRN} Sending error notification from {MessageSource} to CDS.",
            correlationId,
            mrn,
            messageSource
        );

        if (string.IsNullOrWhiteSpace(errorNotification))
            throw new ArgumentException(
                $"{mrn} Request to send an invalid error notification to CDS: {errorNotification}"
            );

        var cdsResponse = await ForwardErrorNotificationAsync(
            mrn,
            messageSource,
            errorNotification,
            correlationId,
            cancellationToken
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
            StatusCode = cdsResponse?.StatusCode ?? HttpStatusCode.NoContent,
            ResponseContent = await GetResponseContentAsync(cdsResponse, cancellationToken),
        };
    }

    private async Task<HttpResponseMessage?> ForwardErrorNotificationAsync(
        string? mrn,
        MessagingConstants.MessageSource messageSource,
        string errorNotification,
        string? correlationId,
        CancellationToken cancellationToken
    )
    {
        if (messageSource == MessagingConstants.MessageSource.Btms)
        {
            _logger.LogDebug(
                "{MessageCorrelationId} {MRN} Sending {MessageSource} Error Notification to CDS.",
                correlationId,
                mrn,
                messageSource
            );

            var response = await SendCdsFormattedSoapMessageAsync(
                errorNotification,
                correlationId,
                _btmsToCdsDestination,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "{MessageCorrelationId} {MRN} Failed to send error notification to CDS. CDS Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                    correlationId,
                    mrn,
                    response.StatusCode,
                    response.ReasonPhrase,
                    await GetResponseContentAsync(response, cancellationToken)
                );
                throw new DecisionException($"{mrn} Failed to send error notification to CDS.");
            }

            _logger.LogInformation(
                "{MessageCorrelationId} {MRN} Successfully sent {MessageSource} Error Notification to CDS.",
                correlationId,
                mrn,
                messageSource
            );

            return response;
        }

        _logger.LogInformation(
            "{MessageCorrelationId} {MRN} {MessageSource} Error Notification sent to NOOP.",
            correlationId,
            mrn,
            messageSource
        );

        return null;
    }
}
