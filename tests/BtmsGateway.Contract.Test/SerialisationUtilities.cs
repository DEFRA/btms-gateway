using System.Text.Json;
using System.Xml.Serialization;

namespace BtmsGateway.Contract.Test
{
    internal static class SerialisationUtilities
    {
        internal static T RoundTripJsonAndXml<T>(T value)
        {
            var json = JsonSerializer.Serialize(value);
            var fromJson = JsonSerializer.Deserialize<T>(json)!;

            var serializer = new XmlSerializer(typeof(T));

            using var reader = new StringReader(ToXml(fromJson));
            return (T)serializer.Deserialize(reader)!;
        }

        internal static string ToXml<T>(T value)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, value);

            return writer.ToString();
        }
    }
}
