using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using Microsoft.FeatureManagement;
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
    private readonly Destination _btmsOutboundErrorsDestination;
    private readonly Destination _alvsOutboundErrorsDestination;
    private readonly Destination _btmsToCdsDestination;
    private readonly IApiSender _apiSender;
    private readonly ILogger _logger;

    public ErrorNotificationSender(
        RoutingConfig? routingConfig,
        IApiSender apiSender,
        IFeatureManager featureManager,
        ILogger logger
    )
        : base(apiSender, routingConfig, logger, featureManager)
    {
        _apiSender = apiSender;
        _logger = logger;

        _btmsOutboundErrorsDestination = GetDestination(MessagingConstants.Destinations.BtmsOutboundErrors);
        _alvsOutboundErrorsDestination = GetDestination(MessagingConstants.Destinations.AlvsOutboundErrors);
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
        _logger.Debug(
            "{MessageCorrelationId} {MRN} Sending error notification from {MessageSource} to Decision Comparer.",
            correlationId,
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

        CheckComparerResponse(comparerResponse, correlationId, mrn, "Error Notification");

        var cdsResponse = await ForwardErrorNotificationAsync(
            mrn,
            messageSource,
            errorNotification,
            correlationId,
            cancellationToken
        );

        return routingResult with
        {
            RouteFound = true,
            RouteLinkType = LinkType.DecisionComparerErrorNotifications,
            ForkLinkType = LinkType.DecisionComparerErrorNotifications,
            RoutingSuccessful = true,
            FullRouteLink = destinationConfig.DestinationUrl,
            FullForkLink = destinationConfig.DestinationUrl,
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
        if (messageSource == await MessageSourceToSend())
        {
            _logger.Debug(
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
                _logger.Error(
                    "{MessageCorrelationId} {MRN} Failed to send error notification to CDS. CDS Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                    correlationId,
                    mrn,
                    response.StatusCode,
                    response.ReasonPhrase,
                    await GetResponseContentAsync(response, cancellationToken)
                );
                throw new DecisionComparisonException($"{mrn} Failed to send error notification to CDS.");
            }

            _logger.Information(
                "{MessageCorrelationId} {MRN} Successfully sent {MessageSource} Error Notification to CDS.",
                correlationId,
                mrn,
                messageSource
            );

            return response;
        }

        _logger.Information(
            "{MessageCorrelationId} {MRN} {MessageSource} Error Notification sent to NOOP.",
            correlationId,
            mrn,
            messageSource
        );

        return null;
    }
}
