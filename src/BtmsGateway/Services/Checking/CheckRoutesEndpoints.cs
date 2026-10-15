using System.Diagnostics.CodeAnalysis;

namespace BtmsGateway.Services.Checking;

[ExcludeFromCodeCoverage]
public static class CheckRoutesEndpoints
{
    public const string Path = "checkroutes";

    public static void UseCheckRoutesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Path, CheckRoutes).AllowAnonymous();
        app.MapGet($"/{Path}/json", CheckRoutesAsJson).AllowAnonymous();
    }

    private static async Task<IResult> CheckRoutes(CheckRoutes checkRoutes)
    {
        var results = await checkRoutes.CheckAll();
        return TypedResults.Text(FormatTextResponse(results));
    }

    private static async Task<IResult> CheckRoutesAsJson(CheckRoutes checkRoutes)
    {
        var results = await checkRoutes.CheckAll();
        return TypedResults.Json(results);
    }

    private static string FormatTextResponse(IEnumerable<CheckRouteResult> results)
    {
        return $"Maximum time for all tracing {Checking.CheckRoutes.OverallTimeoutSecs} secs.\n\n"
            + $"{string.Join('\n', results.Select(result => $"{result.RouteName} - {result.CheckType} - {result.RouteUrl}{(result.HostHeader != null ? $" - Host:{result.HostHeader}" : "")}  [{result.Elapsed.TotalMilliseconds:#,##0.###} ms]\n{string.Join('\n', result.ResponseResult.Split('\n').Select(x => $"{new string(' ', 15)}{x}"))}\n"))}";
    }
}
