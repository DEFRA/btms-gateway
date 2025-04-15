using System.Net;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using ILogger = Serilog.ILogger;
using SlimMessageBus;

namespace BtmsGateway.Consumers;

public class ClearanceDecisionConsumer(RoutingConfig? routingConfig,
    IApiSender apiSender,
    ITradeImportsDataApiClient api,
    ILogger logger) : IConsumer<IConsumerContext<ResourceEvent<CustomsDeclaration>>>
{
    private const string BtmsToCdsDestinationConfigurationKey = "BtmsCds";
    private const string CorrelationIdHeaderName = "CorrelationId";
    private const string AcceptHeaderName = "Accept";

    public async Task OnHandle(IConsumerContext<ResourceEvent<CustomsDeclaration>> context, CancellationToken cancellationToken)
    {
        logger.Information("Clearance Decision received from queue.");

        if (routingConfig?.Destinations?.TryGetValue(BtmsToCdsDestinationConfigurationKey, out var btmsToCdsDestination) ?? false)
        {
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
                        $"{mrn} Customs Declaration does not contain a Clearance Decision.");
                }

                var soapMessage = ClearanceDecisionToSoapConverter.Convert(customsDeclaration.ClearanceDecision, mrn);

                var destination = string.Concat(btmsToCdsDestination.Link, btmsToCdsDestination.RoutePath);
                var headers = new Dictionary<string, string> { { AcceptHeaderName, btmsToCdsDestination.ContentType } };

                if (!string.IsNullOrWhiteSpace(customsDeclaration.ClearanceDecision.ExternalCorrelationId))
                    headers.Add(CorrelationIdHeaderName, customsDeclaration.ClearanceDecision.ExternalCorrelationId);

                var response = await apiSender.SendSoapMessageAsync(
                    btmsToCdsDestination.Method ?? "POST",
                    destination,
                    btmsToCdsDestination.ContentType,
                    btmsToCdsDestination.HostHeader,
                    headers,
                    soapMessage,
                    cancellationToken);

                if (!(response?.IsSuccessStatusCode ?? false))
                {
                    logger.Error("{MRN} Failed to send clearance decision to CDS. CDS Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
                        mrn,
                        response?.StatusCode,
                        response?.ReasonPhrase,
                        await GetResponseContent(response, cancellationToken));
                    throw new ClearanceDecisionProcessingException($"{mrn} Failed to send clearance decision to CDS.");
                }

                logger.Information("{MRN} Clearance Decision successfully processed by ClearanceDecisionConsumer.",
                    mrn);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{MRN} Failed to process clearance decision.", mrn);
                throw new ClearanceDecisionProcessingException($"{mrn} Failed to process clearance decision.", ex);
            }
        }
        else
        {
            logger.Error("BTMS to CDS destination configuration could not be found for processing Clearance Decisions. Please confirm application configuration contains a Destination configuration for {BtmsToCdsDestinationConfigurationKey}",
                BtmsToCdsDestinationConfigurationKey);
            throw new ArgumentException("BTMS to CDS destination configuration could not be found.");
        }
    }

    private static async Task<string> GetResponseContent(HttpResponseMessage? response, CancellationToken cancellationToken)
    {
        if (response is not null && response.StatusCode != HttpStatusCode.NoContent)
            return await response.Content.ReadAsStringAsync(cancellationToken);

        return "No content";
    }
}