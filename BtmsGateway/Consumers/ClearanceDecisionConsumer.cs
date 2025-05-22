using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using SlimMessageBus;

namespace BtmsGateway.Consumers;

public class ClearanceDecisionConsumer(
    ITradeImportsDataApiClient api,
    IDecisionSender decisionSender,
    ILogger<ClearanceDecisionConsumer> logger
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

        logger.LogInformation("Clearance Decision Resource Event received from queue.");

        var mrn = message.ResourceId;

        try
        {
            var customsDeclaration = await api.GetCustomsDeclaration(mrn, cancellationToken);

            if (customsDeclaration is null)
            {
                logger.LogError("{MRN} Customs Declaration not found from Data API.", mrn);
                throw new InvalidOperationException($"{mrn} Customs Declaration not found from Data API.");
            }

            if (customsDeclaration.ClearanceDecision is null)
            {
                logger.LogError("{MRN} Customs Declaration does not contain a Clearance Decision.", mrn);
                throw new InvalidOperationException(
                    $"{mrn} Customs Declaration does not contain a Clearance Decision."
                );
            }

            var soapMessage = ClearanceDecisionToSoapConverter.Convert(customsDeclaration.ClearanceDecision, mrn);

            var result = await decisionSender.SendDecisionAsync(
                mrn,
                soapMessage,
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                externalCorrelationId: customsDeclaration.ClearanceDecision.ExternalCorrelationId,
                cancellationToken: cancellationToken
            );

            if (!result.StatusCode.IsSuccessStatusCode())
            {
                logger.LogError(
                    "{MRN} Failed to send clearance decision to Decision Comparer. Decision Comparer Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                    mrn,
                    result.StatusCode,
                    result.ErrorMessage,
                    result.ResponseContent
                );
                throw new ClearanceDecisionProcessingException(
                    $"{mrn} Failed to send clearance decision to Decision Comparer."
                );
            }

            logger.LogInformation(
                "{MRN} Clearance Decision Resource Event successfully processed by ClearanceDecisionConsumer.",
                mrn
            );
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
