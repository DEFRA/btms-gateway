using BtmsGateway.Config;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Microsoft.Extensions.Options;
using SlimMessageBus;

namespace BtmsGateway.Consumers;

public class ClearanceDecisionConsumer(
    IDecisionSender decisionSender,
    ILogger<ClearanceDecisionConsumer> logger,
    IOptions<CdsOptions> cdsOptions
) : IConsumer<ResourceEvent<CustomsDeclaration>>
{
    public async Task OnHandle(ResourceEvent<CustomsDeclaration> message, CancellationToken cancellationToken)
    {
        if (message.SubResourceType != ResourceEventSubResourceTypes.ClearanceDecision)
        {
            logger.LogInformation(
                "Customs Declaration Sub Resource Type {SubResourceType} skipped.",
                message.SubResourceType
            );
            return;
        }

        var mrn = message.ResourceId;

        logger.LogInformation("{MRN} Clearance Decision Resource Event received from queue.", mrn);

        try
        {
            if (message.Resource is null)
            {
                logger.LogError("{MRN} Customs Declaration Resource Event contained a null resource.", mrn);
                throw new InvalidOperationException(
                    $"{mrn} Customs Declaration Resource Event contained a null resource."
                );
            }

            if (message.Resource.ClearanceDecision is null)
            {
                logger.LogError("{MRN} Customs Declaration does not contain a Clearance Decision.", mrn);
                throw new InvalidOperationException(
                    $"{mrn} Customs Declaration does not contain a Clearance Decision."
                );
            }

            var soapMessage = ClearanceDecisionToSoapConverter.Convert(
                message.Resource.ClearanceDecision,
                mrn,
                cdsOptions.Value.Username,
                cdsOptions.Value.Password
            );

            var result = await decisionSender.SendDecisionAsync(
                mrn,
                soapMessage,
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                correlationId: message.Resource.ClearanceDecision.CorrelationId,
                cancellationToken: cancellationToken
            );

            if (!result.StatusCode.IsSuccessStatusCode())
            {
                logger.LogError(
                    "{MRN} Failed to send clearance decision. Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                    mrn,
                    result.StatusCode,
                    result.ErrorMessage,
                    result.ResponseContent
                );
                throw new ClearanceDecisionProcessingException(
                    $"{mrn} Failed to send clearance decision."
                );
            }

            logger.LogInformation(
                "{MRN} Clearance Decision Resource Event successfully processed by ClearanceDecisionConsumer.",
                mrn
            );
        }
        catch (ConflictException ex)
        {
            logger.LogWarning(ex, "{MRN} Failed to process clearance decision resource event.", mrn);
            throw new ConflictException($"{mrn} Failed to process clearance decision resource event.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{MRN} Failed to process clearance decision resource event.", mrn);
            throw new ClearanceDecisionProcessingException(
                $"{mrn} Failed to process clearance decision resource event.",
                ex
            );
        }
    }
}
