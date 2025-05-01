namespace BtmsGateway.Services.Checking;

public record CheckRouteResult(
    string RouteName,
    string RouteUrl,
    string CheckType,
    string? HostHeader,
    string ResponseResult,
    TimeSpan Elapsed,
    Exception? Exception
);
