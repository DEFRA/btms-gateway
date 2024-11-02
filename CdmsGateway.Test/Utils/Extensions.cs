namespace CdmsGateway.Test.Utils;

public static class Extensions
{
    public static DateTimeOffset RoundDownToSecond(this DateTimeOffset dateTime) => new(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Offset);
}