using System.Text.Json;
using System.Xml.Linq;

namespace BtmsGateway.Services.Converter;

public static class XmlToJsonConverter
{
    public static string Convert(string xml, KnownArray[] knownArrays, string[] knownNumbers)
    {
        return Convert(Validate(xml), knownArrays, knownNumbers);
    }

    public static string Convert(XContainer xContainer, KnownArray[] knownArrays, string[] knownNumbers)
    {
        var jsonObject = new Dictionary<string, object>();
        ConvertElementToDictionary(xContainer, knownArrays, knownNumbers, ref jsonObject);

        return JsonSerializer.Serialize(jsonObject, Json.SerializerOptions);
    }

    private static void ConvertElementToDictionary(XContainer xElement, KnownArray[] knownArrays, string[] knownNumbers, ref Dictionary<string, object> parent)
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
                
                ConvertElementToDictionary(child, knownArrays, knownNumbers, ref childObject);
            }
            else
            {
                dictionary[child.Name.LocalName] = ConvertValue(child, knownNumbers)!;
            }
        }
    }

    private static object? ConvertValue(XElement element, string[] knownNumbers)
    {
        if (element.IsEmpty) return null;
        if (bool.TryParse(element.Value, out var boolResult)) return boolResult;
        if (int.TryParse(element.Value, out var intResult)) return ConvertNumber(element, intResult, knownNumbers);
        if (decimal.TryParse(element.Value, out var decimalResult)) return ConvertNumber(element, decimalResult, knownNumbers);
        return element.Value;
    }

    private static object? ConvertNumber(XElement element, object? result, string[] knownNumbers)
    {
        return knownNumbers.Contains(element.Name.LocalName, StringComparer.InvariantCultureIgnoreCase) ? result : element.Value;
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
