using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using SlimMessageBus;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Consumers;

public class ClearanceDecisionConsumer(ITradeImportsDataApiClient api, IDecisionSender decisionSender, ILogger logger)
    : IConsumer<IConsumerContext<ResourceEvent<CustomsDeclaration>>>
{
    public async Task OnHandle(
        IConsumerContext<ResourceEvent<CustomsDeclaration>> context,
        CancellationToken cancellationToken
    )
    {
        logger.Information("Clearance Decision Resource Event received from queue.");

        if (context.Message is null)
        {
            logger.Error("Invalid message received from queue {Message}.", context.Message);
            throw new InvalidOperationException($"Invalid message received from queue {context.Message}.");
        }

        var mrn = context.Message.ResourceId;

        try
        {
            var customsDeclaration = await api.GetCustomsDeclaration(mrn, cancellationToken);

            if (customsDeclaration is null)
            {
                logger.Error("{MRN} Customs Declaration not found from Data API.", mrn);
                throw new InvalidOperationException($"{mrn} Customs Declaration not found from Data API.");
            }

            if (customsDeclaration.ClearanceDecision is null)
            {
                logger.Error("{MRN} Customs Declaration does not contain a Clearance Decision.", mrn);
                throw new InvalidOperationException(
                    $"{mrn} Customs Declaration does not contain a Clearance Decision."
                );
            }

            var soapMessage = ClearanceDecisionToSoapConverter.Convert(customsDeclaration.ClearanceDecision, mrn);

            var result = await decisionSender.SendDecisionAsync(
                mrn,
                soapMessage,
                MessagingConstants.DecisionSource.Btms,
                cancellationToken: cancellationToken
            );

            if (!result.StatusCode.IsSuccessStatusCode())
            {
                logger.Error(
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

            logger.Information(
                "{MRN} Clearance Decision Resource Event successfully processed by ClearanceDecisionConsumer.",
                mrn
            );
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{MRN} Failed to process clearance decision resource event.", mrn);
            throw new ClearanceDecisionProcessingException(
                $"{mrn} Failed to process clearance decision resource event.",
                ex
            );
        }
    }
}
