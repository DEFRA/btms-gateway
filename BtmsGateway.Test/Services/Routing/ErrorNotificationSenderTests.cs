using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public class ErrorNotificationSenderTests
{
    private RoutingConfig _routingConfig;
    private readonly IApiSender _apiSender = Substitute.For<IApiSender>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly ErrorNotificationSender _errorNotificationSender;

    public ErrorNotificationSenderTests()
    {
        _routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>(),
            NamedLinks = new Dictionary<string, NamedLink>(),
            Destinations = new Dictionary<string, Destination>
            {
                {
                    MessagingConstants.Destinations.BtmsOutboundErrors,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://decision-comparer-url",
                        RoutePath = "/btms-outbound-errors/",
                        ContentType = "application/soap+xml",
                    }
                },
                {
                    MessagingConstants.Destinations.AlvsOutboundErrors,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://decision-comparer-url",
                        RoutePath = "/alvs-outbound-errors/",
                        ContentType = "application/soap+xml",
                    }
                },
            },
        };

        _apiSender
            .SendToDecisionComparerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new HttpResponseMessage(HttpStatusCode.OK));

        _errorNotificationSender = new ErrorNotificationSender(_routingConfig, _apiSender, _logger);
    }

    [Fact]
    public async Task When_sending_btms_error_notification_Then_message_is_sent_to_decision_comparer()
    {
        var result = await _errorNotificationSender.SendErrorNotificationAsync(
            "mrn-123",
            "<HMRCErrorNotification />",
            MessagingConstants.MessageSource.Btms,
            new RoutingResult(),
            cancellationToken: CancellationToken.None
        );

        result.Should().BeAssignableTo<RoutingResult>();
        result
            .Should()
            .BeEquivalentTo(
                new RoutingResult
                {
                    RouteFound = true,
                    RouteLinkType = LinkType.DecisionComparerErrorNotifications,
                    ForkLinkType = LinkType.DecisionComparerErrorNotifications,
                    RoutingSuccessful = true,
                    FullRouteLink = "http://decision-comparer-url/btms-outbound-errors/mrn-123",
                    FullForkLink = "http://decision-comparer-url/btms-outbound-errors/mrn-123",
                    StatusCode = HttpStatusCode.NoContent,
                    ResponseContent = string.Empty,
                }
            );
    }

    [Fact]
    public async Task When_sending_alvs_error_notification_Then_message_is_sent_to_decision_comparer()
    {
        var result = await _errorNotificationSender.SendErrorNotificationAsync(
            "mrn-123",
            "<HMRCErrorNotification />",
            MessagingConstants.MessageSource.Alvs,
            new RoutingResult(),
            cancellationToken: CancellationToken.None
        );

        result.Should().BeAssignableTo<RoutingResult>();
        result
            .Should()
            .BeEquivalentTo(
                new RoutingResult
                {
                    RouteFound = true,
                    RouteLinkType = LinkType.DecisionComparerErrorNotifications,
                    ForkLinkType = LinkType.DecisionComparerErrorNotifications,
                    RoutingSuccessful = true,
                    FullRouteLink = "http://decision-comparer-url/alvs-outbound-errors/mrn-123",
                    FullForkLink = "http://decision-comparer-url/alvs-outbound-errors/mrn-123",
                    StatusCode = HttpStatusCode.NoContent,
                    ResponseContent = string.Empty,
                }
            );
    }

    [Fact]
    public void When_btms_to_decision_comparer_config_has_not_been_set_Then_exception_is_thrown()
    {
        _routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>(),
            NamedLinks = new Dictionary<string, NamedLink>(),
            Destinations = new Dictionary<string, Destination>
            {
                {
                    MessagingConstants.Destinations.AlvsOutboundErrors,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://decision-comparer-url",
                        RoutePath = "/alvs-outbound-errors/",
                        ContentType = "application/soap+xml",
                    }
                },
            },
        };

        var thrownException = Assert.Throws<ArgumentException>(() =>
            new ErrorNotificationSender(_routingConfig, _apiSender, _logger)
        );
        thrownException.Message.Should().Be("Destination configuration could not be found for BtmsOutboundErrors.");
    }

    [Fact]
    public void When_alvs_to_decision_comparer_config_has_not_been_set_Then_exception_is_thrown()
    {
        _routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>(),
            NamedLinks = new Dictionary<string, NamedLink>(),
            Destinations = new Dictionary<string, Destination>
            {
                {
                    MessagingConstants.Destinations.BtmsOutboundErrors,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://decision-comparer-url",
                        RoutePath = "/btms-outbound-errors/",
                        ContentType = "application/soap+xml",
                    }
                },
            },
        };

        var thrownException = Assert.Throws<ArgumentException>(() =>
            new ErrorNotificationSender(_routingConfig, _apiSender, _logger)
        );
        thrownException.Message.Should().Be("Destination configuration could not be found for AlvsOutboundErrors.");
    }

    [Fact]
    public async Task When_sending_an_invalid_error_notification_Then_exception_is_thrown()
    {
        var thrownException = await Assert.ThrowsAsync<ArgumentException>(() =>
            _errorNotificationSender.SendErrorNotificationAsync(
                "mrn-123",
                null,
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                cancellationToken: CancellationToken.None
            )
        );

        thrownException
            .Message.Should()
            .Be("mrn-123 Request to send an invalid error notification to Decision Comparer: ");
    }

    [Fact]
    public async Task When_sending_error_notification_and_comparer_returns_unsuccessful_status_response_Then_exception_is_thrown()
    {
        var comparerResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _apiSender
            .SendToDecisionComparerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IHeaderDictionary>()
            )
            .Returns(comparerResponse);

        var thrownException = await Assert.ThrowsAsync<DecisionComparisonException>(() =>
            _errorNotificationSender.SendErrorNotificationAsync(
                "mrn-123",
                "<HMRCErrorNotification />",
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                cancellationToken: CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Failed to send Error Notification to Decision Comparer.");
    }
}
