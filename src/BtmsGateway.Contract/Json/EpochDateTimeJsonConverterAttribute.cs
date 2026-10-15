using System.Text.Json.Serialization;

namespace BtmsGateway.Contract.Json
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class EpochDateTimeJsonConverterAttribute : JsonConverterAttribute
    {
        public override JsonConverter? CreateConverter(Type typeToConvert)
        {
            return (JsonConverter)new EpochDateTimeJsonConverter();
        }
    }
}
