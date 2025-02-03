using BtmsGateway.Utils;

namespace BtmsGateway.Services.Converter;

public static class JsonToSoapConverter
{
    public static string Convert(string json, KnownArray[] knownArrays, string rootName, bool includeSoapHeader)
    {
        var xDocument = JsonToXmlConverter.ConvertToXdoc(json, knownArrays, rootName);

        return xDocument.ToStringWithDeclaration();
    }
}