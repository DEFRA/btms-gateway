using BtmsGateway.Services.Converter;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class ClearanceDecisionToSoapConverterTests
{
    private static readonly string s_testDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Services",
        "Converter",
        "Fixtures"
    );

    [Fact]
    public void When_receiving_clearance_decision_Then_should_convert_to_soap()
    {
        var expectedSoap = File.ReadAllText(Path.Combine(s_testDataPath, "DecisionNotificationWithHtmlEncoding.xml"));

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

        var result = ClearanceDecisionToSoapConverter.Convert(
            clearanceDecision,
            "25GB1HG99NHUJO3999",
            "test-username",
            "test-password"
        );

        result.Should().Be(expectedSoap);
    }

    [Fact]
    public void When_adding_reason_of_max_length_Then_should_maintain_length()
    {
        var decisionReason = StringOfLength(512);
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
                    Checks =
                    [
                        new ClearanceDecisionCheck
                        {
                            CheckCode = "H219",
                            DecisionCode = "H02",
                            DecisionReasons = [decisionReason],
                        },
                    ],
                },
            ],
        };

        var result = ClearanceDecisionToSoapConverter.Convert(
            clearanceDecision,
            "25GB1HG99NHUJO3999",
            "test-username",
            "test-password"
        );

        result.Should().Contain(decisionReason);
    }

    [Theory]
    [InlineData(513)]
    [InlineData(600)]
    public void When_adding_reason_of_greater_than_max_length_Then_should_truncate(int length)
    {
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
                    Checks =
                    [
                        new ClearanceDecisionCheck
                        {
                            CheckCode = "H219",
                            DecisionCode = "H02",
                            DecisionReasons = [StringOfLength(length)],
                        },
                    ],
                },
            ],
        };

        var result = ClearanceDecisionToSoapConverter.Convert(
            clearanceDecision,
            "25GB1HG99NHUJO3999",
            "test-username",
            "test-password"
        );

        result.Should().Contain(StringOfLength(length)[..509] + "...");
    }

    private static string StringOfLength(int length) => new('a', length);
}
