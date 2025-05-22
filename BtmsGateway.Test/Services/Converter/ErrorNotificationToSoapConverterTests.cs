using BtmsGateway.Services.Converter;
using BtmsGateway.Test.TestUtils;
using Defra.TradeImportsDataApi.Domain.Errors;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class ErrorNotificationToSoapConverterTests
{
    private static readonly string TestDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Services",
        "Converter",
        "Fixtures"
    );

    [Fact]
    public void When_convert_Then_should_return_error_notification_soap_message()
    {
        var expectedSoap = File.ReadAllText(Path.Combine(TestDataPath, "HmrcErrorNotification.xml")).LinuxLineEndings();

        var errorNotification = new ErrorNotification
        {
            ExternalCorrelationId = "12585355",
            Created = DateTime.Parse("2025-03-10T15:43:48.031"),
            ExternalVersion = 1,
            Errors =
            [
                new ErrorItem
                {
                    Code = "ALVSVAL312",
                    Message =
                        "The DocumentControl field (Ex-Heading flag) must be Y or N. Value supplied was null. Your service request with Correlation ID 341735 has been terminated.",
                },
                new ErrorItem
                {
                    Code = "ALVSVAL318",
                    Message =
                        "Item 1 has no Item Document defined for it. ALVS requires at least 1 Item Document. Your service request with Correlation ID 341735 has been terminated.",
                },
                new ErrorItem
                {
                    Code = "ALVSVAL321",
                    Message =
                        "The Item Check for the PHA-POAO Authority is invalid as there are no Item Documents provided for that Authority on ItemNumber 1. Your service request with Correlation ID 341735 has been terminated.",
                },
            ],
        };

        var result = ErrorNotificationToSoapConverter.Convert(errorNotification, "25GB2Q3M9H9K5MSAR8");

        result.Should().Be(expectedSoap);
    }
}
