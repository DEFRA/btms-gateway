using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog;

namespace BtmsGateway.Test.Services.Routing;

public class DecisionSenderTests
{
    private RoutingConfig _routingConfig;
    private readonly IApiSender _apiSender = Substitute.For<IApiSender>();
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
            },
        };

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

        _decisionSender = new DecisionSender(_routingConfig, _apiSender, _logger);
    }

    [Fact]
    public async Task When_sending_decision_Then_message_is_sent_to_comparer_and_comparer_response_sent_onto_cds()
    {
        var result = await _decisionSender.SendDecisionAsync(
            "mrn-123",
            "<DecisionNotification />",
            MessagingConstants.MessageSource.Btms,
            new RoutingResult(),
            new HeaderDictionary(),
            "external-correlation-id",
            CancellationToken.None
        );

        await _apiSender
            .Received(1)
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                "<DecisionNotification />",
                Arg.Any<CancellationToken>()
            );

        result.Should().BeAssignableTo<RoutingResult>();
        result
            .Should()
            .BeEquivalentTo(
                new RoutingResult
                {
                    RouteFound = true,
                    RouteLinkType = LinkType.Url,
                    ForkLinkType = LinkType.Url,
                    RoutingSuccessful = true,
                    FullRouteLink = "http://btms-to-cds-url/route/path-1",
                    FullForkLink = "http://btms-to-cds-url/route/path-1",
                    StatusCode = HttpStatusCode.NoContent,
                    ResponseContent = string.Empty,
                }
            );
    }

    [Fact]
    public void When_btms_to_cds_destination_config_has_not_been_set_Then_exception_is_thrown()
    {
        _routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>(),
            NamedLinks = new Dictionary<string, NamedLink>(),
            Destinations = new Dictionary<string, Destination>(),
        };

        var thrownException = Assert.Throws<ArgumentException>(() =>
            new DecisionSender(_routingConfig, _apiSender, _logger)
        );
        thrownException.Message.Should().Be("Destination configuration could not be found for BtmsCds.");
    }

    [Fact]
    public async Task When_sending_an_invalid_decision_Then_exception_is_thrown()
    {
        var thrownException = await Assert.ThrowsAsync<CdsCommunicationException>(() =>
            _decisionSender.SendDecisionAsync(
                "mrn-123",
                null,
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                cancellationToken: CancellationToken.None
            )
        );

        thrownException.Message.Should().Be("mrn-123 Decision invalid.");
    }

    [Fact]
    public async Task When_sending_decision_and_cds_returns_unsuccessful_response_Then_exception_is_thrown()
    {
        _apiSender
            .SendSoapMessageAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Dictionary<string, string>>(),
                "<DecisionNotification />",
                Arg.Any<CancellationToken>()
            )
            .Returns(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var thrownException = await Assert.ThrowsAsync<CdsCommunicationException>(() =>
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
        var thrownException = await Assert.ThrowsAsync<CdsCommunicationException>(() =>
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

        thrownException.Message.Should().Be("mrn-123 Received decision from unexpected source None.");
    }
}
