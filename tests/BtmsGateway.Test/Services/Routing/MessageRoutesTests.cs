using BtmsGateway.Exceptions;
using BtmsGateway.Services.Converter;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BtmsGateway.Test.Services.Routing;

public class MessageRoutesTests
{
    private static SoapContent GetSoap(string messageName) =>
        new($"<Envelope><Body><{messageName}><Data>{messageName}</Data></{messageName}></Body></Envelope>");

    [Fact]
    public void When_routing_route_1_Then_should_route_correctly()
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, NullLogger<MessageRoutes>.Instance);

        var route = messageRoutes.GetRoute(
            "/route/path-1/sub/path/",
            GetSoap("Message1"),
            "test-correlation-id",
            "test-mrn"
        );

        route.RouteFound.Should().BeTrue();
        route.RouteName.Should().Be("route-1");
        route.MessageSubXPath.Should().Be("Message1");
        route.Legend.Should().Be("Route 1");
        route.RouteLinkType.Should().Be(LinkType.Url);
        route.FullRouteLink.Should().Be("http://legacy-link-url/route/path-1/sub/path");
        route.RouteHostHeader.Should().Be("legacy-host-header");
        route.ConvertRoutedContentToFromJson.Should().BeFalse();
        route.UrlPath.Should().Be("/route/path-1/sub/path");
    }

    [Fact]
    public void When_routing_route_2_Then_should_route_correctly()
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, NullLogger<MessageRoutes>.Instance);

        var route = messageRoutes.GetRoute(
            "/route/path-2/sub/path/",
            GetSoap("Message2"),
            "test-correlation-id",
            "test-mrn"
        );

        route.RouteFound.Should().BeTrue();
        route.RouteName.Should().Be("route-2");
        route.MessageSubXPath.Should().Be("Message2");
        route.RouteLinkType.Should().Be(LinkType.Url);
        route.FullRouteLink.Should().Be("http://btms-link-url/route/path-2/sub/path");
        route.RouteHostHeader.Should().Be("btms-host-header");
        route.ConvertRoutedContentToFromJson.Should().BeTrue();
        route.UrlPath.Should().Be("/route/path-2/sub/path");
    }

    [Fact]
    public void When_routing_route_3_Then_should_route_correctly()
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, NullLogger<MessageRoutes>.Instance);

        var route = messageRoutes.GetRoute(
            "/route/path-3/sub/path/",
            GetSoap("Message3"),
            "test-correlation-id",
            "test-mrn"
        );

        route.RouteFound.Should().BeTrue();
        route.RouteName.Should().Be("route-3");
        route.MessageSubXPath.Should().Be("Message3");
        route.RouteLinkType.Should().Be(LinkType.Url);
        route.FullRouteLink.Should().Be("http://legacy-link-url/route/path-3/sub/path");
        route.RouteHostHeader.Should().Be("legacy-host-header");
        route.ConvertRoutedContentToFromJson.Should().BeFalse();
        route.UrlPath.Should().Be("/route/path-3/sub/path");
    }

    [Fact]
    public void When_routing_route_4_Then_should_route_correctly()
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, NullLogger<MessageRoutes>.Instance);

        var route = messageRoutes.GetRoute(
            "/route/path-4/sub/path/",
            GetSoap("Message4"),
            "test-correlation-id",
            "test-mrn"
        );

        route.RouteFound.Should().BeTrue();
        route.RouteName.Should().Be("route-4");
        route.MessageSubXPath.Should().Be("Message4");
        route.RouteLinkType.Should().Be(LinkType.Url);
        route.FullRouteLink.Should().Be("http://btms-link-url/route/path-4/sub/path");
        route.RouteHostHeader.Should().Be("btms-host-header");
        route.ConvertRoutedContentToFromJson.Should().BeTrue();
        route.UrlPath.Should().Be("/route/path-4/sub/path");
    }

    [Fact]
    public void When_routing_unrecognised_route_Then_should_fail()
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, NullLogger<MessageRoutes>.Instance);

        var route = messageRoutes.GetRoute(
            "/route-99/sub/path/",
            GetSoap("Message1"),
            "test-correlation-id",
            "test-mrn"
        );

        route.RouteFound.Should().BeFalse();
        route.RouteName.Should().BeNull();
        route.FullRouteLink.Should().BeNull();
        route.RouteHostHeader.Should().BeNull();
        route.UrlPath.Should().Be("/route-99/sub/path");
    }

    [Fact]
    public void When_routing_null_route_Then_should_fail()
    {
        var messageRoutes = new MessageRoutes(TestRoutes.RoutingConfig, NullLogger<MessageRoutes>.Instance);

        var route = messageRoutes.GetRoute(null, GetSoap("Message1"), "test-correlation-id", "test-mrn");

        route.RouteFound.Should().BeFalse();
        route.RouteName.Should().BeNull();
        route.FullRouteLink.Should().BeNull();
        route.RouteHostHeader.Should().BeNull();
        route.UrlPath.Should().BeNull();
    }

    [Fact]
    public void When_routing_with_invalid_url_Then_should_fail()
    {
        var act = () => new MessageRoutes(TestRoutes.InvalidUrlConfig, NullLogger<MessageRoutes>.Instance);

        act.Should()
            .Throw<RoutingException>()
            .WithMessage("Error creating routing table: Invalid URL(s) in config")
            .WithInnerException<InvalidDataException>()
            .WithMessage("Invalid URL(s) in config");
    }

    [Fact]
    public void When_routing_with_invalid_route_to_Then_should_fail()
    {
        var act = () => new MessageRoutes(TestRoutes.InvalidRouteToConfig, NullLogger<MessageRoutes>.Instance);

        act.Should()
            .Throw<RoutingException>()
            .WithMessage("Error creating routing table: Invalid Route To in config")
            .WithInnerException<InvalidDataException>()
            .WithMessage("Invalid Route To in config");
    }

    [Fact]
    public void When_routing_with_invalid_link_type_Then_should_fail()
    {
        var act = () => new MessageRoutes(TestRoutes.InvalidLinkTypeConfig, NullLogger<MessageRoutes>.Instance);

        var thrownException = act.Should()
            .Throw<RoutingException>()
            .WithMessage($"Error creating routing table: Invalid Link Type in config")
            .WithInnerException<InvalidDataException>()
            .WithMessage("Invalid Link Type in config");
    }

    [Theory]
    [InlineData("/route/path-1/sub/path", true)]
    [InlineData("/route/path-2/sub/path", false)]
    [InlineData("/foo", false)]
    [InlineData("/foo/", false)]
    public void When_checking_for_cds_route_Then_should_return_expected(string routePath, bool expectedResult)
    {
        var messageRoutes = new MessageRoutes(TestRoutes.CdsDefinedRoutes, NullLogger<MessageRoutes>.Instance);

        messageRoutes.IsCdsRoute(routePath).Should().Be(expectedResult);
    }
}
