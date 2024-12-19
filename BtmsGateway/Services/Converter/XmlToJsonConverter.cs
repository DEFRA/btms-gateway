using System.Text.Json;
using System.Xml.Linq;

namespace BtmsGateway.Services.Converter;

public static class XmlToJsonConverter
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DictionaryKeyPolicy = JsonNamingPolicy.CamelCase };

    public static string Convert(string xml, Dictionary<string, string>? knownArrays = null)
    {
        var xDocument = Validate(xml);
        var jsonObject = new Dictionary<string, object>();
        ConvertElementToDictionary(xDocument, jsonObject, knownArrays ?? []);

        return JsonSerializer.Serialize(jsonObject, JsonSerializerOptions);
    }

    private static void ConvertElementToDictionary(XContainer xElement, Dictionary<string, object> parent, Dictionary<string, string> knownArrays)
    {
        IDictionary<string, object> dictionary = parent;

        foreach (var child in xElement.Elements())
        {
            if (child.HasElements)
            {
                var childObject = new Dictionary<string, object>();
                var elementName = child.Name.LocalName;

                if (knownArrays.TryGetValue(elementName, out var newElementName))
                {
                    if (parent.TryGetValue(newElementName, out var value))
                    {
                        if (value is List<Dictionary<string, object>> list)
                        {
                            list.Add(childObject);
                        }
                    }
                    else
                    {
                        parent[newElementName] = new List<Dictionary<string, object>> { childObject };
                    }
                }
                else
                {
                    parent[elementName] = childObject;
                }
                
                ConvertElementToDictionary(child, childObject, knownArrays);
            }
            else
            {
                dictionary[child.Name.LocalName] = ConvertValue(child)!;
            }
        }
    }

    private static object? ConvertValue(XElement child)
    {
        if (child.IsEmpty) return null;
        if (bool.TryParse(child.Value, out var boolResult)) return boolResult;
        if (int.TryParse(child.Value, out var intResult)) return intResult;
        if (double.TryParse(child.Value, out var doubleResult)) return doubleResult;
        return child.Value;
    }

    private static XDocument Validate(string xml)
    {
        try
        {
            return XDocument.Parse(xml);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("The XML message is not valid", ex);
        }
    }
}
