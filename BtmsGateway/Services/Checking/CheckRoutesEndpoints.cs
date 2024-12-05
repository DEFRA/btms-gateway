namespace BtmsGateway.Services.Checking;

public static class CheckRoutesEndpoints
{
    public const string Path = "checkroutes";
    
    public static void UseCheckRoutesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Path, CheckRoutes).AllowAnonymous();
        app.MapGet($"/{Path}/json", CheckRoutesAsJson).AllowAnonymous();
        app.MapGet($"/{Path}/ipaffs", CheckIpaffsRoutes).AllowAnonymous();
        app.MapGet($"/{Path}/ipaffs/json", CheckIpaffsRoutesAsJson).AllowAnonymous();
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

    private static async Task<IResult> CheckIpaffsRoutes(CheckRoutes checkRoutes)
    {
        var results = await checkRoutes.CheckIpaffs();
        return TypedResults.Text(FormatTextResponse(results));
    }

    private static async Task<IResult> CheckIpaffsRoutesAsJson(CheckRoutes checkRoutes)
    {
        var results = await checkRoutes.CheckIpaffs();
        return TypedResults.Json(results);
    }

    private static string FormatTextResponse(IEnumerable<CheckRouteResult> results)
    {
        return $"Maximum time for all tracing {Checking.CheckRoutes.OverallTimeoutSecs} secs.\n\n" +
               $"{string.Join('\n', results.Select(result => $"{result.RouteName} - {result.CheckType} - {result.RouteUrl}  [{result.Elapsed.TotalMilliseconds:#,##0.###} ms]\n{string.Join('\n', result.ResponseResult.Split('\n').Select(x => $"{new string(' ', 15)}{x}"))}\n"))}";
    }
}