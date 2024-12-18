using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace BtmsGateway.Services.Converter;

public static class XmlToJsonConverter
{
    public static string Convert(string xml)
    {
        Validate(xml);
        WriteToConsole(xml);
        
        var indent = 1;
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        sw.NewLine = "\n";
        var matches = XmlToJsonConverterTypeConversion.XmlRegex().Matches(xml);

        sw.WriteLine("{");
        if (matches.Count > 0) Convert(matches[0], null, sw, ref indent);
        sw.WriteLine("}");

        return sb.ToString().Trim();
    }

    private static void WriteToConsole(string xml)
    {
        var matchNum = 1;
        var matches = XmlToJsonConverterTypeConversion.XmlRegex().Matches(xml);
        foreach (Match match in matches)
        {
            var groupNum = 0;
            Console.WriteLine($"Match {$"{matchNum++,6}"}  {match.Value}");
            foreach (Group group in match.Groups)
            {
                if (groupNum == 0 || !group.Success)
                {
                    groupNum++;
                    continue;
                }
                Console.WriteLine($"Group {(string.IsNullOrWhiteSpace(group.Name) ? groupNum : group.Name),6}  {group.Value}");
                groupNum++;
            }
            Console.WriteLine();
        }
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
    [GeneratedRegex(@"(?>\<|\/)(?'tag'.+?)(?>\>|(?'empty'(?> *?)\/\>))(?'data'.*?)(\<(?'close'\/.+?)\>|$)", RegexOptions.Multiline | RegexOptions.Singleline)]
    public static partial Regex XmlRegex();

    public static bool IsOpeningTag(this Match match) => !match.TagName().StartsWith("/", StringComparison.InvariantCulture) && match.IsNotDataTag();
    
    public static bool IsClosingTag(this Match match) => match.TagName().StartsWith("/", StringComparison.InvariantCulture) && match.IsNotDataTag();

    public static bool IsDataTag(this Match match) => match.Groups["close"].Value.TrimStart('/') == match.TagName() || match.Groups["empty"].Value == "/>";

    public static string TagName(this Match match) => match.Groups["tag"].Value.Trim();

    public static string Data(this Match match) => string.IsNullOrWhiteSpace(match.Groups["data"].Value) ? "null" : $"\"{match.Groups["data"].Value.Trim()}\"";

    private static bool IsNotDataTag(this Match match) => string.IsNullOrWhiteSpace(match.Groups["close"].Value) && string.IsNullOrWhiteSpace(match.Groups["empty"].Value);
    
    public static string Spaces(this int indent) => new(' ', indent * 2);
}