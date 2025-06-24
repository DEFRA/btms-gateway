using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement;
using NSubstitute;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public class ErrorNotificationSenderTests
{
    private RoutingConfig _routingConfig;
    private readonly IApiSender _apiSender = Substitute.For<IApiSender>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly IFeatureManager _featureManager = Substitute.For<IFeatureManager>();
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
                {
                    MessagingConstants.Destinations.BtmsCds,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://cds-url",
                        RoutePath = "/ws/CDS/defra/alvsclearanceinbound/v1",
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

        _apiSender
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new HttpResponseMessage(HttpStatusCode.NoContent));

        _featureManager.IsEnabledAsync(Features.SendOnlyBtmsErrorNotificationToCds).Returns(false);

        _errorNotificationSender = new ErrorNotificationSender(_routingConfig, _apiSender, _featureManager, _logger);
    }

    [Fact]
    public async Task When_sending_alvs_error_notification_Then_message_is_sent_to_decision_comparer_and_cds()
    {
        var result = await _errorNotificationSender.SendErrorNotificationAsync(
            "mrn-123",
            "<HMRCErrorNotification />",
            MessagingConstants.MessageSource.Alvs,
            new RoutingResult(),
            cancellationToken: CancellationToken.None
        );

        await _apiSender
            .Received(1)
            .SendToDecisionComparerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );

        await _apiSender
            .Received(1)
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
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
    public async Task When_feature_not_sending_alvs_error_notification_to_cds_Then_message_is_sent_to_decision_comparer_and_not_cds()
    {
        _featureManager.IsEnabledAsync(Features.SendOnlyBtmsErrorNotificationToCds).Returns(true);

        var result = await _errorNotificationSender.SendErrorNotificationAsync(
            "mrn-123",
            "<HMRCErrorNotification />",
            MessagingConstants.MessageSource.Alvs,
            new RoutingResult(),
            cancellationToken: CancellationToken.None
        );

        await _apiSender
            .Received(1)
            .SendToDecisionComparerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );

        await _apiSender
            .DidNotReceiveWithAnyArgs()
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
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
    public async Task When_sending_btms_error_notification_Then_message_is_sent_to_decision_comparer_and_cds()
    {
        _featureManager.IsEnabledAsync(Features.SendOnlyBtmsErrorNotificationToCds).Returns(true);

        var result = await _errorNotificationSender.SendErrorNotificationAsync(
            "mrn-123",
            "<HMRCErrorNotification />",
            MessagingConstants.MessageSource.Btms,
            new RoutingResult(),
            cancellationToken: CancellationToken.None
        );

        await _apiSender
            .Received(1)
            .SendToDecisionComparerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );

        await _apiSender
            .Received(1)
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
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
    public async Task When_feature_not_sending_btms_error_notification_to_cds_Then_message_is_sent_to_decision_comparer_and_not_cds()
    {
        var result = await _errorNotificationSender.SendErrorNotificationAsync(
            "mrn-123",
            "<HMRCErrorNotification />",
            MessagingConstants.MessageSource.Btms,
            new RoutingResult(),
            cancellationToken: CancellationToken.None
        );

        await _apiSender
            .Received(1)
            .SendToDecisionComparerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );

        await _apiSender
            .DidNotReceiveWithAnyArgs()
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
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
                {
                    MessagingConstants.Destinations.BtmsCds,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://cds-url",
                        RoutePath = "/ws/CDS/defra/alvsclearanceinbound/v1",
                        ContentType = "application/soap+xml",
                    }
                },
            },
        };

        var thrownException = Assert.Throws<ArgumentException>(() =>
            new ErrorNotificationSender(_routingConfig, _apiSender, _featureManager, _logger)
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
                {
                    MessagingConstants.Destinations.BtmsCds,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://cds-url",
                        RoutePath = "/ws/CDS/defra/alvsclearanceinbound/v1",
                        ContentType = "application/soap+xml",
                    }
                },
            },
        };

        var thrownException = Assert.Throws<ArgumentException>(() =>
            new ErrorNotificationSender(_routingConfig, _apiSender, _featureManager, _logger)
        );
        thrownException.Message.Should().Be("Destination configuration could not be found for AlvsOutboundErrors.");
    }

    [Fact]
    public void When_btms_to_cds_config_has_not_been_set_Then_exception_is_thrown()
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

        var thrownException = Assert.Throws<ArgumentException>(() =>
            new ErrorNotificationSender(_routingConfig, _apiSender, _featureManager, _logger)
        );
        thrownException.Message.Should().Be("Destination configuration could not be found for BtmsCds.");
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

    [Fact]
    public async Task When_sending_error_notification_to_cds_fails_Then_exception_is_thrown()
    {
        var cdsResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _apiSender
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(cdsResponse);

        var thrownException = await Assert.ThrowsAsync<DecisionComparisonException>(() =>
            _errorNotificationSender.SendErrorNotificationAsync(
                "mrn-123",
                "<HMRCErrorNotification />",
                MessagingConstants.MessageSource.Alvs,
                new RoutingResult(),
                cancellationToken: CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Failed to send error notification to CDS.");
    }
}
