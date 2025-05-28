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
        var expectedSoap = File.ReadAllText(Path.Combine(TestDataPath, "ClearanceDecisionSoap.xml")).LinuxLineEndings();

        var clearanceDecision = new ClearanceDecision
        {
            ExternalCorrelationId = "external-correlation-id",
            Timestamp = new DateTime(2025, 1, 1),
            ExternalVersionNumber = 1,
            DecisionNumber = 1,
            Items =
            [
                new ClearanceDecisionItem
                {
                    ItemNumber = 1,
                    Checks =
                    [
                        new ClearanceDecisionCheck
                        {
                            CheckCode = "H218",
                            DecisionCode = "C02",
                            DecisionsValidUntil = new DateTime(2025, 1, 1),
                            DecisionReasons = ["Some decision reason"],
                        },
                    ],
                },
                new ClearanceDecisionItem
                {
                    ItemNumber = 2,
                    Checks =
                    [
                        new ClearanceDecisionCheck
                        {
                            CheckCode = "H218",
                            DecisionCode = "C02",
                            DecisionsValidUntil = new DateTime(2025, 1, 1),
                            DecisionReasons = ["Some decision reason 1", "Some decision reason 2"],
                        },
                    ],
                },
                new ClearanceDecisionItem
                {
                    ItemNumber = 3,
                    Checks =
                    [
                        new ClearanceDecisionCheck
                        {
                            CheckCode = "H218",
                            DecisionCode = "C02",
                            DecisionsValidUntil = new DateTime(2025, 1, 1),
                        },
                    ],
                },
                new ClearanceDecisionItem
                {
                    ItemNumber = 4,
                    Checks =
                    [
                        new ClearanceDecisionCheck
                        {
                            CheckCode = "H218",
                            DecisionCode = "C02",
                            DecisionsValidUntil = new DateTime(2025, 1, 1),
                            DecisionReasons = [""],
                        },
                    ],
                },
            ],
        };

        var result = ClearanceDecisionToSoapConverter.Convert(clearanceDecision, "MRN123");

        result.Should().Be(expectedSoap);
    }
}
