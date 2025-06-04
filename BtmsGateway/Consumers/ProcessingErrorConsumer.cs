using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using Defra.TradeImportsDataApi.Domain.Errors;
using Defra.TradeImportsDataApi.Domain.Events;
using SlimMessageBus;

namespace BtmsGateway.Consumers;

public class ProcessingErrorConsumer(
    IErrorNotificationSender errorNotificationSender,
    ILogger<ProcessingErrorConsumer> logger
) : IConsumer<ResourceEvent<ProcessingError>>
{
    public async Task OnHandle(ResourceEvent<ProcessingError> message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing Error Resource Event received from queue.");

        var mrn = message.ResourceId;

        if (message.Resource is null)
        {
            logger.LogWarning("{MRN} Processing Error Resource Event contained a null resource.", mrn);
            return;
        }

        try
        {
            var processingError = message.Resource;

            var soapMessage = ErrorNotificationToSoapConverter.Convert(processingError, mrn);

            var result = await errorNotificationSender.SendErrorNotificationAsync(
                mrn,
                soapMessage,
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                cancellationToken: cancellationToken
            );

            if (!result.StatusCode.IsSuccessStatusCode())
            {
                logger.LogError(
                    "{MRN} Failed to send error notification to Decision Comparer. Decision Comparer Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                    mrn,
                    result.StatusCode,
                    result.ErrorMessage,
                    result.ResponseContent
                );
                throw new ProcessingErrorProcessingException(
                    $"{mrn} Failed to send error notification to Decision Comparer."
                );
            }

            logger.LogInformation(
                "{MRN} Processing Error Resource Event successfully processed by ProcessingErrorConsumer.",
                mrn
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{MRN} Failed to process processing error resource event.", mrn);
            throw new ProcessingErrorProcessingException(
                $"{mrn} Failed to process processing error resource event.",
                ex
            );
        }
    }
}
