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

        _decisionSender = new DecisionSender(_routingConfig, _apiSender, _featureManager, _logger);
    }

    [Theory]
    [InlineData(true, false, MessagingConstants.MessageSource.Btms, 0, "btms-decisions")]
    [InlineData(true, true, MessagingConstants.MessageSource.Btms, 1, "btms-decisions")]
    [InlineData(false, true, MessagingConstants.MessageSource.Btms, 1, "btms-decisions")]
    [InlineData(false, false, MessagingConstants.MessageSource.Btms, 0, "btms-decisions")]
    public async Task When_sending_decision_Then_message_is_sent_to_comparer_and_comparer_response_optionally_sent_onto_cds(
        bool trialCutover,
        bool cutover,
        MessagingConstants.MessageSource messageSource,
        int expectedCallsToCds,
        string expectedCallToComparerDecisions
    )
    {
        _featureManager.IsEnabledAsync(Features.TrialCutover).Returns(trialCutover);
        _featureManager.IsEnabledAsync(Features.Cutover).Returns(cutover);

        var comparerResponse = new HttpResponseMessage(HttpStatusCode.OK);
        comparerResponse.Content = new StringContent("<ComparerDecisionNotification />");

        _apiSender
            .SendToDecisionComparerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IHeaderDictionary>()
            )
            .Returns(comparerResponse);

        var result = await _decisionSender.SendDecisionAsync(
            "mrn-123",
            "<DecisionNotification />",
            messageSource,
            new RoutingResult(),
            new HeaderDictionary(),
            "external-correlation-id",
            CancellationToken.None
        );

        await _apiSender
            .Received(1)
            .SendToDecisionComparerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IHeaderDictionary>()
            );

        await _apiSender
            .Received(expectedCallsToCds)
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                "<ComparerDecisionNotification />",
                Arg.Any<CancellationToken>()
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
                    FullRouteLink = $"http://decision-comparer-url/{expectedCallToComparerDecisions}/mrn-123",
                    FullForkLink = $"http://decision-comparer-url/{expectedCallToComparerDecisions}/mrn-123",
                    StatusCode = HttpStatusCode.NoContent,
                    ResponseContent = string.Empty,
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
            },
        };

        var thrownException = Assert.Throws<ArgumentException>(() =>
            new DecisionSender(_routingConfig, _apiSender, _featureManager, _logger)
        );
        thrownException.Message.Should().Be("Destination configuration could not be found for BtmsDecisionComparer.");
    }

    [Fact]
    public void When_btms_to_cds_destination_config_has_not_been_set_Then_exception_is_thrown()
    {
        _routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>(),
            NamedLinks = new Dictionary<string, NamedLink>(),
            Destinations = new Dictionary<string, Destination>
            {
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
        thrownException.Message.Should().Be("Destination configuration could not be found for BtmsCds.");
    }

    [Fact]
    public async Task When_sending_an_invalid_decision_Then_exception_is_thrown()
    {
        var thrownException = await Assert.ThrowsAsync<ArgumentException>(() =>
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                null,
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
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
            .SendToDecisionComparerAsync(
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
                "<BtmsDecisionNotification />",
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                new HeaderDictionary(),
                "external-correlation-id",
                CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Failed to send Decision to Decision Comparer.");

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
    public async Task When_sending_decision_and_comparer_returns_conflict_status_response_Then_exception_is_thrown()
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
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                "<BtmsDecisionNotification />",
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                new HeaderDictionary(),
                "external-correlation-id",
                CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Failed to send Decision to Decision Comparer.");

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
    public async Task When_sending_decision_and_comparer_returns_invalid_decision_Then_exception_is_thrown()
    {
        _featureManager.IsEnabledAsync(Features.Cutover).Returns(true);

        var comparerResponse = new HttpResponseMessage(HttpStatusCode.OK);
        comparerResponse.Content = new StringContent(string.Empty);

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
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                "<DecisionNotification />",
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                new HeaderDictionary(),
                "external-correlation-id",
                CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Decision Comparer returned an invalid decision.");
    }

    [Fact]
    public async Task When_sending_decision_and_comparer_returns_no_content_Then_exception_is_thrown()
    {
        _featureManager.IsEnabledAsync(Features.Cutover).Returns(true);

        var comparerResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

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
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                "<DecisionNotification />",
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                new HeaderDictionary(),
                "external-correlation-id",
                CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Decision Comparer returned an invalid decision.");
    }

    [Fact]
    public async Task When_sending_decision_and_cds_returns_unsuccessful_response_Then_exception_is_thrown()
    {
        _featureManager.IsEnabledAsync(Features.Cutover).Returns(true);

        var comparerResponse = new HttpResponseMessage(HttpStatusCode.OK);
        comparerResponse.Content = new StringContent("<ComparerDecisionNotification />");

        _apiSender
            .SendToDecisionComparerAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IHeaderDictionary>()
            )
            .Returns(comparerResponse);

        _apiSender
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                "<ComparerDecisionNotification />",
                Arg.Any<CancellationToken>()
            )
            .Returns(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var thrownException = await Assert.ThrowsAsync<DecisionComparisonException>(() =>
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                "<DecisionNotification />",
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                new HeaderDictionary(),
                "external-correlation-id",
                CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Failed to send Decision to CDS.");
    }

    [Fact]
    public async Task When_sending_decision_from_unexpected_source_Then_exception_is_thrown()
    {
        var thrownException = await Assert.ThrowsAsync<ArgumentException>(() =>
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                "<DecisionNotification />",
                MessagingConstants.MessageSource.None,
                new RoutingResult(),
                new HeaderDictionary(),
                "external-correlation-id",
                CancellationToken.None
            )
        );

        thrownException.Message.Should().Be($"mrn-123 Received decision from unexpected source None.");
    }
}
