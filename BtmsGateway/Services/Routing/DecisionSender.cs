using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Utils;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Routing;

public interface IDecisionSender
{
    Task<RoutingResult> SendDecisionAsync(
        string? mrn,
        string? decision,
        MessagingConstants.DecisionSource decisionSource,
        string? externalCorrelationId = null,
        CancellationToken cancellationToken = default);
}

public class DecisionSender(RoutingConfig? routingConfig, IApiSender apiSender, ILogger logger) : IDecisionSender
{
    private const string CorrelationIdHeaderName = "CorrelationId";
    private const string AcceptHeaderName = "Accept";
    
    public async Task<RoutingResult> SendDecisionAsync(
        string? mrn,
        string? decision,
        MessagingConstants.DecisionSource decisionSource,
        string? externalCorrelationId = null,
        CancellationToken cancellationToken = default)
    {
        logger.Debug("{MRN} Sending decision from {DecisionSource} to Decision Comparer.", mrn, decisionSource);

        if (!(routingConfig?.Destinations.TryGetValue(MessagingConstants.Destinations.BtmsCds, out var btmsToCdsDestination) ?? false)
            || !(routingConfig?.Destinations.TryGetValue(MessagingConstants.Destinations.BtmsDecisionComparer, out var btmsDecisionsComparerDestination) ?? false)
            || !(routingConfig?.Destinations.TryGetValue(MessagingConstants.Destinations.AlvsDecisionComparer, out var alvsDecisionComparerDestination) ?? false))
        {
            logger.Error("{MRN} Destination configuration could not be found for processing Decisions. Please confirm application configuration contains a Destination configuration for {BtmsCdsConfigKey}, {BtmsDecisionComparerConfigKey} and {AlvsDecisionComparerConfigKey}.",
                mrn,
                MessagingConstants.Destinations.BtmsCds,
                MessagingConstants.Destinations.BtmsDecisionComparer,
                MessagingConstants.Destinations.AlvsDecisionComparer);
            throw new ArgumentException("Destination configuration could not be found for processing Decisions.");
        }

        if (decision is null)
            throw new ArgumentException($"{mrn} Invalid Decision received {decision}");

        var comparerResponse = decisionSource switch
        {
            MessagingConstants.DecisionSource.Btms => await apiSender.SendDecisionAsync(
                decision,
                $"{btmsDecisionsComparerDestination.Link}{btmsDecisionsComparerDestination.RoutePath}{mrn}",
                btmsDecisionsComparerDestination.ContentType,
                cancellationToken),
            MessagingConstants.DecisionSource.Alvs => await apiSender.SendDecisionAsync(
                decision,
                $"{alvsDecisionComparerDestination.Link}{alvsDecisionComparerDestination.RoutePath}{mrn}",
                alvsDecisionComparerDestination.ContentType,
                cancellationToken),
            _ => throw new ArgumentException($"{mrn} Received decision from unexpected source {decisionSource}.")
        };

        if (!comparerResponse.StatusCode.IsSuccessStatusCode())
        {
            logger.Error("{MRN} Failed to send Decision to Decision Comparer: Status Code: {ComparerResponseStatusCode}, Reason: {ComparerResponseReason}.",
                mrn,
                comparerResponse.StatusCode,
                comparerResponse.ReasonPhrase);
            throw new DecisionComparisonException($"{mrn} Failed to send Decision to Decision Comparer.");
        }
        
        if (decisionSource == MessagingConstants.DecisionSource.Alvs)
        {
            var comparerDecision = await GetResponseContentAsync(comparerResponse, cancellationToken);
            logger.Debug("{MRN} Received Decision from Decision Comparer {ComparerDecision}", mrn, comparerDecision);
            // Just log decision for now. Eventually, in cut over, will send the Decision to CDS.
            // await SendToCds(mrn, comparerDecision, externalCorrelationId, btmsToCdsDestination, cancellationToken);
        }

        return new RoutingResult
        {
            RouteFound = true,
            RouteLinkType = LinkType.DecisionComparer,
            RoutingSuccessful = true,
            FullRouteLink = decisionSource == MessagingConstants.DecisionSource.Btms ?
                $"{btmsDecisionsComparerDestination.Link}{btmsDecisionsComparerDestination.RoutePath}{mrn}" :
                $"{alvsDecisionComparerDestination.Link}{alvsDecisionComparerDestination.RoutePath}{mrn}",
            StatusCode = comparerResponse.StatusCode,
            ResponseContent = "Decision Comparer Result"
        };
    }

    // private async Task SendToCds(
    //     string? mrn,
    //     string soapMessage,
    //     string? externalCorrelationId,
    //     Destination btmsToCdsDestination,
    //     CancellationToken cancellationToken)
    // {
    //     var destination = string.Concat(btmsToCdsDestination.Link, btmsToCdsDestination.RoutePath);
    //     var headers = new Dictionary<string, string> { { AcceptHeaderName, btmsToCdsDestination.ContentType } };
    //
    //     if (!string.IsNullOrWhiteSpace(externalCorrelationId))
    //         headers.Add(CorrelationIdHeaderName, externalCorrelationId);
    //
    //     var response = await apiSender.SendSoapMessageAsync(
    //         btmsToCdsDestination.Method ?? "POST",
    //         destination,
    //         btmsToCdsDestination.ContentType,
    //         btmsToCdsDestination.HostHeader,
    //         headers,
    //         soapMessage,
    //         cancellationToken);
    //
    //     if (!(response?.IsSuccessStatusCode ?? false))
    //     {
    //         logger.Error("{MRN} Failed to send clearance decision to CDS. CDS Response Status Code: {StatusCode}, Reason: {Reason}, Content: {Content}",
    //             mrn,
    //             response?.StatusCode,
    //             response?.ReasonPhrase,
    //             await GetResponseContent(response, cancellationToken));
    //         throw new DecisionComparisonException($"{mrn} Failed to send clearance decision to CDS.");
    //     }
    // }
    
    private static async Task<string> GetResponseContentAsync(HttpResponseMessage? response, CancellationToken cancellationToken)
    {
        if (response is not null && response.StatusCode != HttpStatusCode.NoContent)
            return await response.Content.ReadAsStringAsync(cancellationToken);
    
        return "No content";
    }
}