using System.Xml.Linq;
using BtmsGateway.Domain;
using BtmsGateway.Services.Converter;
using FluentAssertions;

namespace BtmsGateway.Test.Services.Converter;

public class SoapUtilsTests
{
    [Theory]
    [InlineData("ALVSClearanceRequest", "http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com")]
    [InlineData("DecisionNotification", "http://www.hmrc.gov.uk/webservices/itsw/ws/decisionnotification")]
    [InlineData("FinalisationNotificationRequest", "http://notifyfinalisedstatehmrcfacade.types.esb.ws.cara.defra.com")]
    [InlineData("ALVSErrorNotificationRequest", "http://alvserrornotification.types.esb.ws.cara.defra.com")]
    [InlineData("HMRCErrorNotification", "http://www.hmrc.gov.uk/webservices/itsw/ws/hmrcerrornotification")]
    public void When_getting_root_attribute_value_Then_expected_namespace_should_be_returned(
        string rootName,
        string expectedNamespace
    )
    {
        var result = SoapUtils.GetRootAttributeValue(rootName);

        result.Should().Be(expectedNamespace);
    }

    [Fact]
    public void When_getting_root_attribute_value_for_unknown_root_Then_it_should_throw()
    {
        var act = () => SoapUtils.GetRootAttributeValue("UnknownRoot");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(MessagingConstants.SoapMessageTypes.ALVSClearanceRequest, "ALVSClearanceResponse")]
    [InlineData(
        MessagingConstants.SoapMessageTypes.FinalisationNotificationRequest,
        "FinalisationNotificationResponse"
    )]
    [InlineData(MessagingConstants.SoapMessageTypes.ALVSErrorNotificationRequest, "ALVSErrorNotificationResponse")]
    public void When_getting_success_response_for_known_message_type_Then_response_body_should_be_returned(
        string messageSubXPath,
        string expectedElement
    )
    {
        var result = SoapUtils.GetMessageTypeSuccessResponse(messageSubXPath);

        result.Should().NotBeNull();
        result.Should().Contain(expectedElement);
    }

    [Fact]
    public void When_getting_success_response_for_unknown_message_type_Then_it_should_return_null()
    {
        var result = SoapUtils.GetMessageTypeSuccessResponse("Unknown");

        result.Should().BeNull();
    }

    [Fact]
    public void When_adding_alvs_to_cds_envelope_Then_username_and_password_should_be_added()
    {
        var rootElement = new XElement("DecisionNotification", new XElement("ServiceHeader"));

        var envelope = SoapUtils.AddSoapEnvelope(rootElement, SoapType.AlvsToCds, "user", "pass");

        XNamespace oasNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        envelope.Descendants(oasNs + "Username").Single().Value.Should().Be("user");
        envelope.Descendants(oasNs + "Password").Single().Value.Should().Be("pass");
    }
}
