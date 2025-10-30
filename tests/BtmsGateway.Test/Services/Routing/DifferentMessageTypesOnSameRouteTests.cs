using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BtmsGateway.Test.Services.Routing;

public class DifferentMessageTypesOnSameRouteTests
{
    private static SoapContent CreateSoap(string messageName) =>
        new(
            $"<?xml version=\"1.0\" encoding=\"utf-8\"?><s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><m:{messageName} xmlns:m=\"http://local1\"><n:Message xmlns:n=\"http://local3\"><Data xmlns=\"http://local2\">111</Data></n:Message></m:{messageName}></s:Body></s:Envelope>"
        );

    [Fact]
    public void When_routing_first_message_type_to_first_route_without_attributes_Then_should_reach_first_target_route()
    {
        var messageRoutes = new MessageRoutes(
            TestRoutes.DifferentMessageTypesOnSameRoutingConfig,
            NullLogger<MessageRoutes>.Instance
        );

        var route = messageRoutes.GetRoute(
            "/route/path-A/sub/path",
            CreateSoap("Message1"),
            "test-correlation-id",
            "test-mrn"
        );

        route.RouteFound.Should().BeTrue();
        route.RouteName.Should().Be("route-1");
        route.FullRouteLink.Should().Be("http://legacy-link-1-url/route/path-A/sub/path");
    }

    [Fact]
    public void When_routing_second_message_type_to_first_route_without_attributes_Then_should_reach_first_target_route()
    {
        var messageRoutes = new MessageRoutes(
            TestRoutes.DifferentMessageTypesOnSameRoutingConfig,
            NullLogger<MessageRoutes>.Instance
        );

        var route = messageRoutes.GetRoute(
            "/route/path-A/sub/path",
            CreateSoap("Message2"),
            "test-correlation-id",
            "test-mrn"
        );

        route.RouteFound.Should().BeTrue();
        route.RouteName.Should().Be("route-2");
        route.FullRouteLink.Should().Be("http://legacy-link-2-url/route/path-A/sub/path");
    }
}
