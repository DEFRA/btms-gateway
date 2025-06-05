using BtmsGateway.Services.Converter;
using BtmsGateway.Test.TestUtils;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class ClearanceDecisionToSoapConverterTests
{
    private static readonly string TestDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Services",
        "Converter",
        "Fixtures"
    );

    [Fact]
    public void When_receiving_clearance_decision_Then_should_convert_to_soap()
    {
        var expectedSoap = File.ReadAllText(Path.Combine(TestDataPath, "DecisionNotificationWithHtmlEncoding.xml"))
            .LinuxLineEndings();

        var clearanceDecision = new ClearanceDecision
        {
            CorrelationId = "000",
            Created = DateTime.Parse("2025-05-29T18:57:29.298"),
            ExternalVersionNumber = 3,
            DecisionNumber = 3,
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks = [new ClearanceDecisionCheck { CheckCode = "H219", DecisionCode = "H02" }],
                },
            ],
        };

        var result = ClearanceDecisionToSoapConverter.Convert(clearanceDecision, "25GB1HG99NHUJO3999");

        result.Should().Be(expectedSoap);
    }
}
