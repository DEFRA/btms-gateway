using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Utils;
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
        CancellationToken cancellationToken = default
    );
}

public class ErrorNotificationSender : SoapMessageSenderBase, IErrorNotificationSender
{
    private readonly Destination _btmsOutboundErrorsDestination;
    private readonly Destination _alvsOutboundErrorsDestination;
    private readonly IApiSender _apiSender;
    private readonly ILogger _logger;

    public ErrorNotificationSender(RoutingConfig? routingConfig, IApiSender apiSender, ILogger logger)
        : base(routingConfig, logger)
    {
        _apiSender = apiSender;
        _logger = logger;

        _btmsOutboundErrorsDestination = GetDestination(MessagingConstants.Destinations.BtmsOutboundErrors);
        _alvsOutboundErrorsDestination = GetDestination(MessagingConstants.Destinations.AlvsOutboundErrors);
    }

    public async Task<RoutingResult> SendErrorNotificationAsync(
        string? mrn,
        string? errorNotification,
        MessagingConstants.MessageSource messageSource,
        RoutingResult routingResult,
        IHeaderDictionary? headers = null,
        CancellationToken cancellationToken = default
    )
    {
        _logger.Debug(
            "{MRN} Sending error notification from {MessageSource} to Decision Comparer.",
            mrn,
            messageSource
        );

        if (string.IsNullOrWhiteSpace(errorNotification))
            throw new ArgumentException(
                $"{mrn} Request to send an invalid error notification to Decision Comparer: {errorNotification}"
            );

        var destinationConfig = messageSource switch
        {
            MessagingConstants.MessageSource.Btms => GetDestinationConfiguration(mrn, _btmsOutboundErrorsDestination),
            MessagingConstants.MessageSource.Alvs => GetDestinationConfiguration(mrn, _alvsOutboundErrorsDestination),
            _ => throw new ArgumentException(
                $"{mrn} Received error notification from unexpected source {messageSource}."
            ),
        };

        var comparerResponse = await _apiSender.SendToDecisionComparerAsync(
            errorNotification,
            destinationConfig.DestinationUrl,
            destinationConfig.ContentType,
            cancellationToken,
            headers
        );

        if (!comparerResponse.StatusCode.IsSuccessStatusCode())
        {
            _logger.Error(
                "{MRN} Failed to send Error Notification to Decision Comparer: Status Code: {ComparerResponseStatusCode}, Reason: {ComparerResponseReason}.",
                mrn,
                comparerResponse.StatusCode,
                comparerResponse.ReasonPhrase
            );
            throw new DecisionComparisonException($"{mrn} Failed to send Error Notification to Decision Comparer.");
        }

        ForwardErrorNotificationAsync(mrn, messageSource, errorNotification);

        return routingResult with
        {
            RouteFound = true,
            RouteLinkType = LinkType.DecisionComparerErrorNotifications,
            ForkLinkType = LinkType.DecisionComparerErrorNotifications,
            RoutingSuccessful = true,
            FullRouteLink = destinationConfig.DestinationUrl,
            FullForkLink = destinationConfig.DestinationUrl,
            StatusCode = HttpStatusCode.NoContent,
            ResponseContent = string.Empty,
        };
    }

    private void ForwardErrorNotificationAsync(string? mrn, MessagingConstants.MessageSource messageSource, string _)
    {
        if (messageSource == MessagingConstants.MessageSource.Btms)
        {
            _logger.Information("{MRN} Produced Error Notification to send to CDS", mrn);
            // Just log error notification for now. Eventually, in cut over, will send the notification to CDS.
            // Ensure original ALVS request headers are passed through and appended in the CDS request!
            // Pass the CDS response back out!
        }
    }
}
