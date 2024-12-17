using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace BtmsGateway.Services.Converter;

public static class XmlToJsonConverter
{
    public static string Convert(string xml)
    {
        Validate(xml);
        var indent = 1;
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb) { NewLine = "\n" };
        var matches = XmlToJsonConverterTypeConversion.XmlRegex().Matches(xml);

        sw.WriteLine("{");
        if (matches.Count > 0) Convert(matches[0], null, sw, ref indent);
        sw.WriteLine("}");

        return sb.ToString().Trim();
    }

    private static void Convert(Match match, Match? previousMatch, StringWriter sw, ref int indent)
    {
        var xmlToJsonOpen = new XmlToJsonOpen(match, sw);
        var previousTagMayRequireComma = previousMatch?.IsClosingTag() == true || previousMatch?.IsDataTag() == true;
        xmlToJsonOpen.Convert(ref indent, previousTagMayRequireComma);
        if (match.NextMatch().Success) Convert(match.NextMatch(), match, sw, ref indent);
    }

    private static void Validate(string xml)
    {
        try 
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("The XML message is not valid", ex);
        }    
    }
}

public abstract class XmlToJsonTag(XmlToJsonTag? next = null)
{
    protected abstract bool Check { get; }
    protected abstract void ConvertLine(ref int indent, bool previousTagRequiresComma);

    public void Convert(ref int indent, bool previousTagMayRequireComma)
    {
        if (Check) 
            ConvertLine(ref indent, previousTagMayRequireComma);
        else 
            next?.Convert(ref indent, previousTagMayRequireComma);
    }
}

public class XmlToJsonOpen(Match match, StringWriter sw) : XmlToJsonTag(new XmlToJsonClose(match, sw))
{
    protected override bool Check => match.IsOpeningTag();
    protected override void ConvertLine(ref int indent, bool previousTagRequiresComma)
    {
        var sb = sw.GetStringBuilder();
        sb.Insert(sb.Length - 1, previousTagRequiresComma ? "," : null);
        sw.WriteLine($"{(indent++).Spaces()}\"{match.TagName()}\": {{");
    }
}

public class XmlToJsonClose(Match match, StringWriter sw) : XmlToJsonTag(new XmlToJsonData(match, sw))
{
    protected override bool Check => match.IsClosingTag();
    protected override void ConvertLine(ref int indent, bool _) => sw.WriteLine($"{(--indent).Spaces()}}}");
}

public class XmlToJsonData(Match match, StringWriter sw) : XmlToJsonTag
{
    protected override bool Check => match.IsDataTag();
    protected override void ConvertLine(ref int indent, bool previousTagRequiresComma)
    {
        var sb = sw.GetStringBuilder();
        sb.Insert(sb.Length - 1, previousTagRequiresComma ? "," : null);
        sw.WriteLine($"{indent.Spaces()}\"{match.TagName()}\": {match.Data()}");
    }
}

public static partial class XmlToJsonConverterTypeConversion
{
    [GeneratedRegex(@"(?>\<|\/)(.+?)(?>\>|((?> *?)\/\>))(.*?)(\<(\/.+?)\>|$)", RegexOptions.Multiline)]
    public static partial Regex XmlRegex();

    public static bool IsOpeningTag(this Match match) => !match.TagName().StartsWith("/", StringComparison.InvariantCulture) && match.IsNotDataTag();
    
    public static bool IsClosingTag(this Match match) => match.TagName().StartsWith("/", StringComparison.InvariantCulture) && match.IsNotDataTag();
    
    public static bool IsDataTag(this Match match) => match.Groups[5].Value.TrimStart('/') == match.TagName() || match.Groups[2].Value == "/>";

    public static string Spaces(this int indent) => new(' ', indent * 2);

    public static string TagName(this Match match) => match.Groups[1].Value.Trim();

    public static string Data(this Match match) => string.IsNullOrWhiteSpace(match.Groups[3].Value) ? "null" : $"\"{match.Groups[3].Value}\"";

    private static bool IsNotDataTag(this Match match) => string.IsNullOrWhiteSpace(match.Groups[5].Value) && string.IsNullOrWhiteSpace(match.Groups[2].Value);
}