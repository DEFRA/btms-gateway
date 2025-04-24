using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using NSubstitute;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public class DecisionSenderTests
{
    private RoutingConfig _routingConfig;
    private readonly IApiSender _apiSender = Substitute.For<IApiSender>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private DecisionSender _decisionSender;

    public DecisionSenderTests()
    {
        _routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>(),
            NamedLinks = new Dictionary<string, NamedLink>(),
            Destinations = new Dictionary<string, Destination>
            {
                { MessagingConstants.Destinations.BtmsCds, new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://btms-to-cds-url",
                        RoutePath = "/route/path-1",
                        ContentType = "application/soap+xml",
                        HostHeader = "syst32.hmrc.gov.uk",
                        Method = "POST"
                    }
                },
                { MessagingConstants.Destinations.BtmsDecisionComparer, new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://decision-comparer-url",
                        RoutePath = "/btms-decisions/",
                        ContentType = "application/soap+xml"
                    }
                },
                { MessagingConstants.Destinations.AlvsDecisionComparer, new Destination
                    {
                        LinkType = LinkType.Url,
                        Link = "http://decision-comparer-url",
                        RoutePath = "/alvs-decisions/",
                        ContentType = "application/soap+xml"
                    }
                }
            }
        };

        _apiSender.SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage(HttpStatusCode.OK));

        _apiSender.SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage(HttpStatusCode.OK));

        _decisionSender = new DecisionSender(_routingConfig, _apiSender, _logger);
    }

    [Fact]
    public async Task When_sending_btms_decision_Then_decision_is_sent_to_comparer_and_not_onto_cds()
    {
        var result = await _decisionSender.SendDecisionAsync("mrn", "<BtmsDecisionNotification />",
            MessagingConstants.DecisionSource.Btms, "external-correlation-id", CancellationToken.None);

        result.Should().BeAssignableTo<RoutingResult>();
        result.Should().BeEquivalentTo(new RoutingResult
        {
            RouteFound = true,
            RouteLinkType = LinkType.DecisionComparer,
            RoutingSuccessful = true,
            FullRouteLink = "http://decision-comparer-url/btms-decisions/mrn",
            StatusCode = HttpStatusCode.OK,
            ResponseContent = "Decision Comparer Result"
        });

        await _apiSender.DidNotReceive().SendSoapMessageAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_sending_alvs_decision_Then_decision_is_sent_to_comparer_and_comparer_response_sent_onto_cds()
    {
        var comparerResponse = new HttpResponseMessage(HttpStatusCode.OK);
        comparerResponse.Content = new StringContent("<ComparerDecisionNotification />");

        _apiSender.SendDecisionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(comparerResponse);

        var result = await _decisionSender.SendDecisionAsync("mrn", "<AlvsDecisionNotification />",
            MessagingConstants.DecisionSource.Alvs, "external-correlation-id", CancellationToken.None);

        result.Should().BeAssignableTo<RoutingResult>();
        result.Should().BeEquivalentTo(new RoutingResult
        {
            RouteFound = true,
            RouteLinkType = LinkType.DecisionComparer,
            RoutingSuccessful = true,
            FullRouteLink = "http://decision-comparer-url/alvs-decisions/mrn",
            StatusCode = HttpStatusCode.OK,
            ResponseContent = "Decision Comparer Result"
        });

        // await _apiSender.Received(1).SendSoapMessageAsync(
        //     Arg.Any<string>(),
        //     Arg.Any<string>(),
        //     Arg.Any<string>(),
        //     Arg.Any<string>(),
        //     Arg.Any<Dictionary<string, string>>(),
        //     Arg.Is<string>(x => x == "<ComparerDecisionNotification />"),
        //     Arg.Any<CancellationToken>());
    }

    // [Fact]
    // public async Task When_destination_config_has_not_been_set_Then_exception_is_thrown()
    // {
    //     _routingConfig = new()
    //     {
    //         NamedRoutes = new Dictionary<string, NamedRoute>(),
    //         NamedLinks = new Dictionary<string, NamedLink>(),
    //         Destinations = new Dictionary<string, Destination>()
    //     };
    //     _consumer = new ClearanceDecisionConsumer(_routingConfig, _apiSender, _tradeImportsDataApiClient, _logger);
    //
    //     await Assert.ThrowsAsync<ArgumentException>(() => _consumer.OnHandle(_context, CancellationToken.None));
    // }

    // [Fact]
    // public async Task When_sending_to_cds_returns_no_response_Then_exception_is_thrown()
    // {
    //     _apiSender.SendSoapMessageAsync(
    //             Arg.Any<string>(),
    //             Arg.Any<string>(),
    //             Arg.Any<string>(),
    //             Arg.Any<string>(),
    //             Arg.Any<Dictionary<string, string>>(),
    //             Arg.Any<string>(),
    //             Arg.Any<CancellationToken>())
    //         .ReturnsNull();
    //
    //     var thrownException = await Assert.ThrowsAsync<ClearanceDecisionProcessingException>(() => _consumer.OnHandle(_context, CancellationToken.None));
    //     thrownException.Message.Should().Be("24GB123456789AB012 Failed to process clearance decision resource event.");
    //     thrownException.InnerException.Should().BeAssignableTo<ClearanceDecisionProcessingException>();
    //     thrownException.InnerException?.Message.Should().Be("24GB123456789AB012 Failed to send clearance decision to CDS.");
    // }
}