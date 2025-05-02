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

public class DecisionSenderTests
{
    private RoutingConfig _routingConfig;
    private readonly IApiSender _apiSender = Substitute.For<IApiSender>();
    private readonly IFeatureManager _featureManager = Substitute.For<IFeatureManager>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly DecisionSender _decisionSender;

    public DecisionSenderTests()
    {
        _routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>(),
            NamedLinks = new Dictionary<string, NamedLink>(),
            Destinations = new Dictionary<string, Destination>
            {
                {
                    MessagingConstants.Destinations.BtmsCds,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://btms-to-cds-url",
                        RoutePath = "/route/path-1",
                        ContentType = "application/soap+xml",
                        HostHeader = "syst32.hmrc.gov.uk",
                        Method = "POST",
                    }
                },
                {
                    MessagingConstants.Destinations.BtmsDecisionComparer,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://decision-comparer-url",
                        RoutePath = "/btms-decisions/",
                        ContentType = "application/soap+xml",
                    }
                },
                {
                    MessagingConstants.Destinations.AlvsDecisionComparer,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://decision-comparer-url",
                        RoutePath = "/alvs-decisions/",
                        ContentType = "application/soap+xml",
                    }
                },
            },
        };

        _apiSender
            .SendDecisionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
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
            .Returns(new HttpResponseMessage(HttpStatusCode.OK));
        
        _featureManager.IsEnabledAsync(Features.SendBtmsDecisionToCds).Returns(false);

        _decisionSender = new DecisionSender(_routingConfig, _apiSender, _featureManager, _logger);
    }

    [Fact]
    public async Task When_sending_btms_decision_Then_decision_is_sent_to_comparer_and_comparer_response_is_not_sent_onto_cds()
    {
        var result = await _decisionSender.SendDecisionAsync(
            "mrn-123",
            "<BtmsDecisionNotification />",
            MessagingConstants.DecisionSource.Btms,
            cancellationToken: CancellationToken.None
        );

        result.Should().BeAssignableTo<RoutingResult>();
        result
            .Should()
            .BeEquivalentTo(
                new RoutingResult
                {
                    RouteFound = true,
                    RouteLinkType = LinkType.DecisionComparer,
                    ForkLinkType = LinkType.DecisionComparer,
                    RoutingSuccessful = true,
                    FullRouteLink = "http://decision-comparer-url/btms-decisions/mrn-123",
                    FullForkLink = "http://decision-comparer-url/btms-decisions/mrn-123",
                    StatusCode = HttpStatusCode.OK,
                    ResponseContent = "Decision Comparer Result",
                }
            );

        await _apiSender
            .DidNotReceive()
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task When_sending_alvs_decision_Then_decision_is_sent_to_comparer_and_comparer_response_sent_onto_cds()
    {
        var comparerResponse = new HttpResponseMessage(HttpStatusCode.OK);
        comparerResponse.Content = new StringContent("<ComparerDecisionNotification />");

        _apiSender
            .SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IHeaderDictionary>()
            )
            .Returns(comparerResponse);

        var result = await _decisionSender.SendDecisionAsync(
            "mrn-123",
            "<AlvsDecisionNotification />",
            MessagingConstants.DecisionSource.Alvs,
            new HeaderDictionary(),
            CancellationToken.None
        );

        result.Should().BeAssignableTo<RoutingResult>();
        result
            .Should()
            .BeEquivalentTo(
                new RoutingResult
                {
                    RouteFound = true,
                    RouteLinkType = LinkType.DecisionComparer,
                    ForkLinkType = LinkType.DecisionComparer,
                    RoutingSuccessful = true,
                    FullRouteLink = "http://decision-comparer-url/alvs-decisions/mrn-123",
                    FullForkLink = "http://decision-comparer-url/alvs-decisions/mrn-123",
                    StatusCode = HttpStatusCode.OK,
                    ResponseContent = "Decision Comparer Result",
                }
            );
    }

    [Fact]
    public void When_btms_to_decision_comparer_destination_config_has_not_been_set_Then_exception_is_thrown()
    {
        _routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>(),
            NamedLinks = new Dictionary<string, NamedLink>(),
            Destinations = new Dictionary<string, Destination>
            {
                {
                    MessagingConstants.Destinations.BtmsCds,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://btms-to-cds-url",
                        RoutePath = "/route/path-1",
                        ContentType = "application/soap+xml",
                        HostHeader = "syst32.hmrc.gov.uk",
                        Method = "POST",
                    }
                },
                {
                    MessagingConstants.Destinations.AlvsDecisionComparer,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://decision-comparer-url",
                        RoutePath = "/alvs-decisions/",
                        ContentType = "application/soap+xml",
                    }
                },
            },
        };

        var thrownException = Assert.Throws<ArgumentException>(() =>
            new DecisionSender(_routingConfig, _apiSender, _featureManager, _logger)
        );
        thrownException.Message.Should().Be("Destination configuration could not be found for BtmsDecisionComparer.");
    }

    [Fact]
    public void When_alvs_to_decision_comparer_destination_config_has_not_been_set_Then_exception_is_thrown()
    {
        _routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>(),
            NamedLinks = new Dictionary<string, NamedLink>(),
            Destinations = new Dictionary<string, Destination>
            {
                {
                    MessagingConstants.Destinations.BtmsCds,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://btms-to-cds-url",
                        RoutePath = "/route/path-1",
                        ContentType = "application/soap+xml",
                        HostHeader = "syst32.hmrc.gov.uk",
                        Method = "POST",
                    }
                },
                {
                    MessagingConstants.Destinations.BtmsDecisionComparer,
                    new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://decision-comparer-url",
                        RoutePath = "/btms-decisions/",
                        ContentType = "application/soap+xml",
                    }
                },
            },
        };

        var thrownException = Assert.Throws<ArgumentException>(() =>
            new DecisionSender(_routingConfig, _apiSender, _featureManager, _logger)
        );
        thrownException.Message.Should().Be("Destination configuration could not be found for AlvsDecisionComparer.");
    }

    [Fact]
    public async Task When_sending_an_invalid_decision_Then_exception_is_thrown()
    {
        var thrownException = await Assert.ThrowsAsync<ArgumentException>(() =>
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                null,
                MessagingConstants.DecisionSource.Btms,
                cancellationToken: CancellationToken.None
            )
        );

        thrownException.Message.Should().Be("mrn-123 Request to send an invalid decision to Decision Comparer: ");
    }

    [Fact]
    public async Task When_sending_decision_and_comparer_returns_unsuccessful_status_response_Then_exception_is_thrown()
    {
        var comparerResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _apiSender
            .SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IHeaderDictionary>()
            )
            .Returns(comparerResponse);

        var thrownException = await Assert.ThrowsAsync<DecisionComparisonException>(() =>
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                "<AlvsDecisionNotification />",
                MessagingConstants.DecisionSource.Alvs,
                new HeaderDictionary(),
                CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Failed to send Decision to Decision Comparer.");
    }

    [Fact]
    public async Task When_sending_decision_and_comparer_returns_invalid_decision_Then_exception_is_thrown()
    {
        var comparerResponse = new HttpResponseMessage(HttpStatusCode.OK);
        comparerResponse.Content = new StringContent(string.Empty);

        _apiSender
            .SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IHeaderDictionary>()
            )
            .Returns(comparerResponse);

        var thrownException = await Assert.ThrowsAsync<DecisionComparisonException>(() =>
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                "<AlvsDecisionNotification />",
                MessagingConstants.DecisionSource.Alvs,
                new HeaderDictionary(),
                CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Decision Comparer returned an invalid decision.");
    }

    [Fact]
    public async Task When_sending_decision_and_comparer_returns_no_content_Then_exception_is_thrown()
    {
        var comparerResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        _apiSender
            .SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IHeaderDictionary>()
            )
            .Returns(comparerResponse);

        var thrownException = await Assert.ThrowsAsync<DecisionComparisonException>(() =>
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                "<AlvsDecisionNotification />",
                MessagingConstants.DecisionSource.Alvs,
                new HeaderDictionary(),
                CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Decision Comparer returned an invalid decision.");
    }

    [Fact]
    public async Task When_send_btms_decision_to_cds_feature_is_enabled_Then_only_btms_decision_is_sent_to_cds()
    {
        // TODO
    }
}
