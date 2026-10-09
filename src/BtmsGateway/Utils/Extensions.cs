using System.Net;
using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace BtmsGateway.Utils;

public static class Extensions
{
    public static T? ConfigureToType<T>(this WebApplicationBuilder builder, string? sectionName = null)
        where T : class
    {
        sectionName ??= typeof(T).Name.Replace("Config", "");
        var configSection = builder.Configuration.GetSection(sectionName);
        builder.Services.Configure<T>(configSection);
        builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<T>>().Value);
        return configSection.Get<T>();
    }

    public static string ToTitleCase(this string text) => char.ToUpper(text[0]) + text[1..];

    public static string ToStringWithDeclaration(this XDocument xDocument, XNamespace rootNs) =>
        $"{xDocument.Declaration}{xDocument.ToString(SaveOptions.DisableFormatting)}".Replace(
            "\"" + rootNs + "\"",
            "&quot;" + rootNs + "&quot;"
        );

    public static bool IsSuccessStatusCode(this HttpStatusCode statusCode) =>
        (int)statusCode >= 200 && (int)statusCode <= 299;
}
