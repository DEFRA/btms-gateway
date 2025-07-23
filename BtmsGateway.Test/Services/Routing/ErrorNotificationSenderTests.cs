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

        _errorNotificationSender = new ErrorNotificationSender(_routingConfig, _apiSender, _featureManager, _logger);
    }

    [Theory]
    [InlineData(true, false, MessagingConstants.MessageSource.Alvs, 1, "alvs-outbound-errors")]
    [InlineData(true, false, MessagingConstants.MessageSource.Btms, 0, "btms-outbound-errors")]
    [InlineData(true, true, MessagingConstants.MessageSource.Alvs, 0, "alvs-outbound-errors")]
    [InlineData(true, true, MessagingConstants.MessageSource.Btms, 1, "btms-outbound-errors")]
    [InlineData(false, true, MessagingConstants.MessageSource.Alvs, 0, "alvs-outbound-errors")]
    [InlineData(false, true, MessagingConstants.MessageSource.Btms, 1, "btms-outbound-errors")]
    [InlineData(false, false, MessagingConstants.MessageSource.Alvs, 0, "alvs-outbound-errors")]
    [InlineData(false, false, MessagingConstants.MessageSource.Btms, 0, "btms-outbound-errors")]
    public async Task When_sending_error_notification_Then_message_is_sent_to_decision_comparer_and_optionally_cds(
        bool trialCutover,
        bool cutover,
        MessagingConstants.MessageSource messageSource,
        int expectedCallsToCds,
        string expectedCallToOutboundErrors
    )
    {
        _featureManager.IsEnabledAsync(Features.TrialCutover).Returns(trialCutover);
        _featureManager.IsEnabledAsync(Features.Cutover).Returns(cutover);

        var result = await _errorNotificationSender.SendErrorNotificationAsync(
            "mrn-123",
            "<HMRCErrorNotification />",
            messageSource,
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
            .Received(expectedCallsToCds)
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
                    FullRouteLink = $"http://decision-comparer-url/{expectedCallToOutboundErrors}/mrn-123",
                    FullForkLink = $"http://decision-comparer-url/{expectedCallToOutboundErrors}/mrn-123",
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

        _logger
            .Received(1)
            .Error(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<HttpStatusCode>(),
                Arg.Any<string>()
            );
        _logger
            .DidNotReceiveWithAnyArgs()
            .Warning(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<HttpStatusCode>(),
                Arg.Any<string>()
            );
    }

    [Fact]
    public async Task When_sending_error_notification_and_comparer_returns_conflict_status_response_Then_exception_is_thrown()
    {
        var comparerResponse = new HttpResponseMessage(HttpStatusCode.Conflict);

        _apiSender
            .SendToDecisionComparerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IHeaderDictionary>()
            )
            .Returns(comparerResponse);

        var thrownException = await Assert.ThrowsAsync<ConflictException>(() =>
            _errorNotificationSender.SendErrorNotificationAsync(
                "mrn-123",
                "<HMRCErrorNotification />",
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                cancellationToken: CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Failed to send Error Notification to Decision Comparer.");

        _logger
            .Received(1)
            .Warning(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<HttpStatusCode>(),
                Arg.Any<string>()
            );
        _logger
            .DidNotReceiveWithAnyArgs()
            .Error(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<HttpStatusCode>(),
                Arg.Any<string>()
            );
    }

    [Fact]
    public async Task When_sending_error_notification_to_cds_fails_Then_exception_is_thrown()
    {
        _featureManager.IsEnabledAsync(Features.TrialCutover).Returns(true);

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
