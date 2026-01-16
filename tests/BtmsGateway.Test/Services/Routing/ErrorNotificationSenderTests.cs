using System.Net;
using BtmsGateway.Domain;
using BtmsGateway.Exceptions;
using BtmsGateway.Services.Routing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BtmsGateway.Test.Services.Routing;

public class ErrorNotificationSenderTests
{
    private RoutingConfig _routingConfig;
    private readonly IApiSender _apiSender = Substitute.For<IApiSender>();
    private readonly ILogger<ErrorNotificationSender> _logger = NullLogger<ErrorNotificationSender>.Instance;
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
                    MessagingConstants.Destinations.BtmsCds,
                    new Destination
                    {
                        Link = "http://cds-url",
                        RoutePath = "/ws/CDS/defra/alvsclearanceinbound/v1",
                        ContentType = "application/soap+xml",
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

        _errorNotificationSender = new ErrorNotificationSender(_routingConfig, _apiSender, _logger);
    }

    [Fact]
    public async Task When_sending_error_notification_Then_message_is_sent_to_decision_comparer_and_cds()
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
                    RoutingSuccessful = true,
                    FullRouteLink = "http://cds-url/ws/CDS/defra/alvsclearanceinbound/v1",
                    StatusCode = HttpStatusCode.NoContent,
                    ResponseContent = string.Empty,
                }
            );
    }

    [Fact]
    public void When_btms_to_cds_config_has_not_been_set_Then_exception_is_thrown()
    {
        _routingConfig = new RoutingConfig
        {
            NamedRoutes = new Dictionary<string, NamedRoute>(),
            NamedLinks = new Dictionary<string, NamedLink>(),
            Destinations = new Dictionary<string, Destination>(),
        };

        var thrownException = Assert.Throws<ArgumentException>(() =>
            new ErrorNotificationSender(_routingConfig, _apiSender, _logger)
        );
        thrownException.Message.Should().Be("Destination configuration could not be found for BtmsCds.");
    }

    [Fact]
    public async Task When_sending_an_invalid_error_notification_Then_exception_is_thrown()
    {
        var thrownException = await Assert.ThrowsAsync<CdsCommunicationException>(() =>
            _errorNotificationSender.SendErrorNotificationAsync(
                "mrn-123",
                null,
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                cancellationToken: CancellationToken.None
            )
        );

        thrownException.Message.Should().Be("mrn-123 Request to send an invalid error notification to CDS: ");
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

        var thrownException = await Assert.ThrowsAsync<CdsCommunicationException>(() =>
            _errorNotificationSender.SendErrorNotificationAsync(
                "mrn-123",
                "<HMRCErrorNotification />",
                MessagingConstants.MessageSource.Btms,
                new RoutingResult(),
                cancellationToken: CancellationToken.None
            )
        );
        thrownException.Message.Should().Be("mrn-123 Failed to send error notification to CDS.");
    }
}
