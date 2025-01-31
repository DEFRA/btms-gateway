using System.Text.Json;
using System.Xml.Linq;

namespace BtmsGateway.Services.Converter;

public static class XmlToJsonConverter
{
    public static string Convert(string xml, KnownArray[] knownArrays)
    {
        return Convert(Validate(xml), knownArrays);
    }

    public static string Convert(XContainer xContainer, KnownArray[] knownArrays)
    {
        var jsonObject = new Dictionary<string, object>();
        ConvertElementToDictionary(xContainer, knownArrays, ref jsonObject);

        return JsonSerializer.Serialize(jsonObject, Json.SerializerOptions);
    }

    private static void ConvertElementToDictionary(XContainer xElement, KnownArray[] knownArrays, ref Dictionary<string, object> parent)
    {
        IDictionary<string, object> dictionary = parent;

        foreach (var child in xElement.Elements())
        {
            if (child.HasElements)
            {
                var childObject = new Dictionary<string, object>();
                var elementName = child.Name.LocalName;

                var newElementName = knownArrays.SingleOrDefault(x => x.ItemName == elementName)?.ArrayName;
                if (newElementName != null)
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
                
                ConvertElementToDictionary(child, knownArrays, ref childObject);
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
