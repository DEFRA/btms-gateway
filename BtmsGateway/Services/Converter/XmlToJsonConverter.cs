using System.Text.Json;
using System.Xml.Linq;

namespace BtmsGateway.Services.Converter;

public static class XmlToJsonConverter
{
    public static string Convert(string xml)
    {
        return Convert(Validate(xml));
    }

    public static string Convert(XContainer xContainer)
    {
        var jsonObject = new Dictionary<string, object>();
        ConvertElementToDictionary(xContainer, ref jsonObject);

        return JsonSerializer.Serialize(jsonObject, Json.SerializerOptions);
    }

    private static void ConvertElementToDictionary(XContainer xElement, ref Dictionary<string, object> parent)
    {
        IDictionary<string, object> dictionary = parent;

        foreach (var child in xElement.Elements())
        {
            if (child.HasElements)
            {
                var childObject = new Dictionary<string, object>();
                var elementName = child.Name.LocalName;

                var newElementName = DomainInfo.KnownArrays.SingleOrDefault(x => x.ItemName == elementName)?.ArrayName;
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
                
                ConvertElementToDictionary(child, ref childObject);
            }
            else
            {
                dictionary[child.Name.LocalName] = ConvertValue(child)!;
            }
        }
    }

    private static object? ConvertValue(XElement element)
    {
        if (element.IsEmpty) return null;
        if (bool.TryParse(element.Value, out var boolResult)) return boolResult;
        if (int.TryParse(element.Value, out var intResult)) return ConvertNumber(element, intResult);
        if (long.TryParse(element.Value, out var longResult)) return ConvertNumber(element, longResult);
        if (decimal.TryParse(element.Value, out var decimalResult)) return ConvertNumber(element, decimalResult);
        return element.Value;
    }

    private static object? ConvertNumber(XElement element, object? result)
    {
        return DomainInfo.KnownNumbers.Contains(element.Name.LocalName, StringComparer.InvariantCultureIgnoreCase) ? result : element.Value;
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
