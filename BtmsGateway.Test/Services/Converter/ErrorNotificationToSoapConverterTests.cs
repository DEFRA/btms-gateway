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
        var expectedSoap = File.ReadAllText(Path.Combine(TestDataPath, "HmrcErrorNotificationWithHtmlEncoding.xml"))
            .LinuxLineEndings();

        var errorNotification = new ProcessingError
        {
            CorrelationId = "000",
            SourceExternalCorrelationId = "101",
            Created = DateTime.Parse("2025-05-29T19:10:13.259"),
            ExternalVersion = 2,
            Errors =
            [
                new ErrorItem
                {
                    Code = "ALVSVAL303",
                    Message =
                        "The Import Declaration was processed as an Amendment. However the EntryReference 25GB1HG99NHUBO3999 and EntryVersionNumber 2 for Owning Department HMRC (NB Code 100=HMRC. If any other code, please contact the ALVS support team at IBM) is already known to ALVS as a currently active Import Declaration. Your service request with Correlation ID 1748545811 has been terminated.",
                },
            ],
        };

        var result = ErrorNotificationToSoapConverter.Convert(errorNotification, "25GB1HG99NHUBO3999");

        result.Should().Be(expectedSoap);
    }
}
