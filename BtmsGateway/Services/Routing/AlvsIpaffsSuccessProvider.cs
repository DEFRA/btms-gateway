using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Services.Converter;

namespace BtmsGateway.Services.Routing;

public interface IAlvsIpaffsSuccessProvider
{
    RoutingResult SendIpaffsRequest(RoutingResult routingResult);
}

public class AlvsIpaffsSuccessProvider : IAlvsIpaffsSuccessProvider
{
    public RoutingResult SendIpaffsRequest(RoutingResult routingResult)
    {
        var (responseContent, status) = routingResult.MessageSubXPath switch
        {
            MessagingConstants.SoapMessageTypes.ALVSIPAFFSClearanceRequest => (
                SoapUtils.AlvsIpaffsClearanceRequestSuccessfulResponseBody,
                HttpStatusCode.OK
            ),
            MessagingConstants.SoapMessageTypes.ALVSIPAFFSFinalisationNotificationRequest => (
                SoapUtils.AlvsIpaffsFinalisationSuccessfulResponseBody,
                HttpStatusCode.OK
            ),
            MessagingConstants.SoapMessageTypes.ALVSIPAFFSDecisionNotification => (
                SoapUtils.AlvsIpaffsDecisionNotificationSuccessfulResponseBody,
                HttpStatusCode.OK
            ),
            _ => (string.Empty, HttpStatusCode.NoContent),
        };

        return new RoutingResult
        {
            RoutingSuccessful = true,
            ResponseContent = responseContent,
            StatusCode = status,
        };
    }
}
