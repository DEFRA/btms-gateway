namespace BtmsGateway.Test.TestUtils;

public static class Extensions
{
    public static DateTimeOffset RoundDownToSecond(this DateTimeOffset dateTime) => new(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Offset);

    public static string LinuxLineEndings(this string text) => text.Replace("\r\n", "\n");
}