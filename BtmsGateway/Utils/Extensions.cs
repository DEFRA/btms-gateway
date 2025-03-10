using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace BtmsGateway.Utils;

public static class Extensions
{
    public static void ConfigureToType<T>(this WebApplicationBuilder builder, string? sectionName = null) where T : class
    {
        sectionName ??= typeof(T).Name.Replace("Config", "");
        builder.Services.Configure<T>(builder.Configuration.GetSection(sectionName));
        builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<T>>().Value);
    }

    public static string ToTitleCase(this string text) => char.ToUpper(text[0]) + text[1..];

    public static string ToStringWithDeclaration(this XDocument xDocument) => $"{xDocument.Declaration}{Environment.NewLine}{xDocument}";
}