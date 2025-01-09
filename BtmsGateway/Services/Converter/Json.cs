using System.Text.Json;

namespace BtmsGateway.Services.Converter;

public static class Json
{
    public static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DictionaryKeyPolicy = JsonNamingPolicy.CamelCase };
}