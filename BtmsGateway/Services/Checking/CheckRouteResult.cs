namespace BtmsGateway.Services.Checking;

public record CheckRouteResult(string RouteName, string RouteUrl, string CheckType, string ResponseResult, TimeSpan Elapsed);