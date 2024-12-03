namespace BtmsGateway.Services.Checking;

public static class CheckRoutesEndpoints
{
    public const string Path = "checkroutes";
    
    public static void UseCheckRoutesEndpoints(this IEndpointRouteBuilder app)
    {
        // app.MapGet(Path, CheckRoutes).AllowAnonymous();
        // app.MapGet($"/{Path}/json", CheckRoutesAsJson).AllowAnonymous();
    }

    // private static async Task<IResult> CheckRoutes(CheckRoutes checkRoutes)
    // {
    //     var results = await checkRoutes.Check();
    //     return TypedResults.Text($"Maximum time for all tracing {Checking.CheckRoutes.OverallTimeoutSecs} secs.\n\n" +
    //                              $"{string.Join('\n', results.Select(result => $"{result.RouteName} - {result.CheckType} - {result.RouteUrl}  [{result.Elapsed.TotalMilliseconds:#,##0.###} ms]\n{string.Join('\n', result.ResponseResult.Split('\n').Select(x => $"{new string(' ', 15)}{x}"))}\n"))}");
    // }
    //
    // private static async Task<IResult> CheckRoutesAsJson(CheckRoutes checkRoutes)
    // {
    //     var results = await checkRoutes.Check();
    //     return TypedResults.Json(results);
    // }
}