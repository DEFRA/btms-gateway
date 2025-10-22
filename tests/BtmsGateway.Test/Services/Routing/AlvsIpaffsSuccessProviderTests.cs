using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Routing;

public class AlvsIpaffsSuccessProviderTests
{
    private readonly AlvsIpaffsSuccessProvider _alvsIpaffsSuccessProvider = new();

    [Fact]
    public void WhenSendingClearanceRequest_ThenShouldReceiveSuccessResponse()
    {
        var routingResult = new RoutingResult
        {
            MessageSubXPath = MessagingConstants.SoapMessageTypes.ALVSIPAFFSClearanceRequest,
        };

        var result = _alvsIpaffsSuccessProvider.SendIpaffsRequest(routingResult);

        result.RoutingSuccessful.Should().BeTrue();
        result.ResponseContent.Should().Be(SoapUtils.AlvsIpaffsClearanceRequestSuccessfulResponseBody);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void WhenSendingFinalisation_ThenShouldReceiveSuccessResponse()
    {
        var routingResult = new RoutingResult
        {
            MessageSubXPath = MessagingConstants.SoapMessageTypes.ALVSIPAFFSFinalisationNotificationRequest,
        };

        var result = _alvsIpaffsSuccessProvider.SendIpaffsRequest(routingResult);

        result.RoutingSuccessful.Should().BeTrue();
        result.ResponseContent.Should().Be(SoapUtils.AlvsIpaffsFinalisationSuccessfulResponseBody);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void WhenSendingDecisionNotification_ThenShouldReceiveSuccessResponse()
    {
        var routingResult = new RoutingResult
        {
            MessageSubXPath = MessagingConstants.SoapMessageTypes.ALVSIPAFFSDecisionNotification,
        };

        var result = _alvsIpaffsSuccessProvider.SendIpaffsRequest(routingResult);

        result.RoutingSuccessful.Should().BeTrue();
        result.ResponseContent.Should().Be(SoapUtils.AlvsIpaffsDecisionNotificationSuccessfulResponseBody);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void WhenSendingAnyOtherRequest_ThenShouldReceiveSuccessResponse()
    {
        var routingResult = new RoutingResult
        {
            MessageSubXPath = MessagingConstants.SoapMessageTypes.ALVSClearanceRequest,
        };

        var result = _alvsIpaffsSuccessProvider.SendIpaffsRequest(routingResult);

        result.RoutingSuccessful.Should().BeTrue();
        result.ResponseContent.Should().Be(string.Empty);
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
