using BtmsGateway.Config;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using Defra.TradeImportsDataApi.Domain.Errors;
using Defra.TradeImportsDataApi.Domain.Events;
using Microsoft.Extensions.Options;
using SlimMessageBus;

namespace BtmsGateway.Consumers;

public class ProcessingErrorConsumer(
    IErrorNotificationSender errorNotificationSender,
    ILogger<ProcessingErrorConsumer> logger,
    IOptions<CdsOptions> cdsOptions
) : IConsumer<ResourceEvent<ProcessingErrorResource>>
{
    public async Task OnHandle(ResourceEvent<ProcessingErrorResource> message, CancellationToken cancellationToken)
    {
        var mrn = message.ResourceId;

        logger.LogInformation("{MRN} Processing Error Resource Event received from queue.", mrn);

        if (message.Resource is null)
        {
            logger.LogWarning("{MRN} Processing Error Resource Event contained a null resource.", mrn);
            return;
        }

        try
        {
            var processingErrors = message.Resource.ProcessingErrors;

            var latestProcessingError = processingErrors
                .OrderBy(processingError => processingError.Created)
                .LastOrDefault();

            if (latestProcessingError is null)
            {
                logger.LogWarning("{MRN} Processing Errors contained no processing errors.", mrn);
                return;
            }

            latestProcessingError.Errors = [.. latestProcessingError.Errors.Where(e => e.Code.StartsWith("ALVSVAL"))];

            if (latestProcessingError.Errors.Length == 0)
            {
                logger.LogDebug("{MRN} Processing Errors only contained non-ALVSVAL errors", mrn);
                return;
            }

            var soapMessage = ErrorNotificationToSoapConverter.Convert(
                latestProcessingError,
                mrn,
                cdsOptions.Value.Username,
                cdsOptions.Value.Password
            );

            var result = await errorNotificationSender.SendErrorNotificationAsync(
                mrn,
                soapMessage,
                MessagingConstants.MessageSource.Btms,
                RoutingResult.Empty,
                correlationId: latestProcessingError.CorrelationId,
                cancellationToken: cancellationToken
            );

            if (!result.StatusCode.IsSuccessStatusCode())
            {
                logger.LogError(
                    "{MRN} Failed to send error notification. Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                    mrn,
                    result.StatusCode,
                    result.ErrorMessage,
                    result.ResponseContent
                );
                throw new ProcessingErrorProcessingException(
                    $"{mrn} Failed to send error notification."
                );
            }

            logger.LogInformation(
                "{MRN} Processing Error Resource Event successfully processed by ProcessingErrorConsumer.",
                mrn
            );
        }
        catch (ConflictException ex)
        {
            logger.LogWarning(ex, "{MRN} Failed to process processing error resource event.", mrn);
            throw new ConflictException($"{mrn} Failed to process processing error resource event.", ex);
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
