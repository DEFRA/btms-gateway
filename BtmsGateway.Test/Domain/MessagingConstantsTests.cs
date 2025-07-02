using BtmsGateway.Domain;
using FluentAssertions;

namespace BtmsGateway.Test.Domain;

public class MessagingConstantsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("FOO")]
    public void When_soap_message_type_is_null_Then_message_type_is_unknown(string? soapMessageType)
    {
        MessagingConstants.MessageTypes.FromSoapMessageType(soapMessageType).Should().Be("UnknownMessageType");
    }
}