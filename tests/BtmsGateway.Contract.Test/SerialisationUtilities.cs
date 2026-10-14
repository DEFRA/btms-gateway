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
            using var writer = new StringWriter();
            serializer.Serialize(writer, fromJson);

            using var reader = new StringReader(writer.ToString());
            return (T)serializer.Deserialize(reader)!;
        }
    }
}
