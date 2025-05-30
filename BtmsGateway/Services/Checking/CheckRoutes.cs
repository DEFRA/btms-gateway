using System.Diagnostics;
using BtmsGateway.Utils.Http;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Checking;

public class CheckRoutes(
    HealthCheckConfig healthCheckConfig,
    IHttpClientFactory clientFactory,
    ILogger logger,
    IProcessRunner processRunner
)
{
    public const int OverallTimeoutSecs = 50;

    public async Task<IEnumerable<CheckRouteResult>> CheckAll()
    {
        if (healthCheckConfig.Disabled)
            return [];

        logger.Debug("Start route checking");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(OverallTimeoutSecs));
        var checkRouteResults = await Task.WhenAll(
            healthCheckConfig.Urls.Select(GetCheckRouteUrl).Where(x => !x.Disabled).Select(x => CheckAll(x, cts))
        );
        return checkRouteResults.SelectMany(routeResults => routeResults);
    }

    public async Task<IEnumerable<CheckRouteResult>> CheckIpaffs()
    {
        logger.Debug("Start route checking");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(OverallTimeoutSecs));
        return (
            await Task.WhenAll(
                healthCheckConfig
                    .Urls.Where(x => !x.Value.Disabled && x.Key.StartsWith("IPAFFS"))
                    .Select(x => CheckAll(GetCheckRouteUrl(x), cts))
            )
        ).SelectMany(routeResults => routeResults);
    }

    private static CheckRouteUrl GetCheckRouteUrl(KeyValuePair<string, HealthCheckUrl> x)
    {
        return new CheckRouteUrl
        {
            Name = x.Key,
            Method = x.Value.Method,
            Disabled = x.Value.Disabled,
            HostHeader = x.Value.HostHeader,
            Url = x.Value.Url,
            CheckType = "HTTP",
        };
    }

    private async Task<IEnumerable<CheckRouteResult>> CheckAll(CheckRouteUrl checkRouteUrl, CancellationTokenSource cts)
    {
        var checks = new List<Task<CheckRouteResult>>
        {
            CheckHttp(checkRouteUrl, false, cts.Token),
            CheckNsLookup(checkRouteUrl, cts.Token),
            CheckDig(checkRouteUrl, cts.Token),
        };
        var hostOnlyUrl = $"{checkRouteUrl.Uri.Scheme}://{checkRouteUrl.Uri.Host}";
        if (checkRouteUrl.Url != hostOnlyUrl)
            checks.Insert(
                1,
                CheckHttp(
                    checkRouteUrl with
                    {
                        CheckType = "HTTP HOST",
                        Url = $"{checkRouteUrl.Uri.Scheme}://{checkRouteUrl.Uri.Host}",
                    },
                    false,
                    cts.Token
                )
            );

        return await Task.WhenAll(checks);
    }

    private async Task<CheckRouteResult> CheckHttp(
        CheckRouteUrl checkRouteUrl,
        bool includeResponseBody,
        CancellationToken token
    )
    {
        var checkRouteResult = new CheckRouteResult(
            checkRouteUrl.Name,
            $"{checkRouteUrl.Method} {checkRouteUrl.Url}",
            checkRouteUrl.CheckType,
            checkRouteUrl.HostHeader,
            string.Empty,
            TimeSpan.Zero,
            null
        );
        var stopwatch = new Stopwatch();

        try
        {
            logger.Debug("Start checking HTTP request for {Url}", checkRouteUrl.Url);
            var client = clientFactory.CreateClient(Proxy.ProxyClientWithoutRetry);
            var request = new HttpRequestMessage(new HttpMethod(checkRouteUrl.Method), checkRouteUrl.Url);
            if (checkRouteUrl.HostHeader != null)
                request.Headers.TryAddWithoutValidation("host", checkRouteUrl.HostHeader);
            stopwatch.Start();
            var response = await client.SendAsync(request, token);
            checkRouteResult = checkRouteResult with
            {
                ResponseResult =
                    $"{response.StatusCode.ToString()} ({(int)response.StatusCode}){(includeResponseBody ? $"\n{await response.Content.ReadAsStringAsync(token)}" : string.Empty)}",
                Elapsed = stopwatch.Elapsed,
            };
        }
        catch (Exception ex)
        {
            checkRouteResult = GetCheckRouteResultWithException(checkRouteResult, ex, stopwatch);
        }

        stopwatch.Stop();
        logger.Debug(
            checkRouteResult.Exception,
            "Completed checking HTTP request for {Url} with result {Result}",
            checkRouteUrl.Url,
            checkRouteResult.ResponseResult
        );

        return checkRouteResult;
    }

    private Task<CheckRouteResult> CheckNsLookup(CheckRouteUrl checkRouteUrl, CancellationToken token) =>
        CheckWithProcess(checkRouteUrl.Name, "nslookup", checkRouteUrl.Uri.Host, token);

    private Task<CheckRouteResult> CheckDig(CheckRouteUrl checkRouteUrl, CancellationToken token) =>
        CheckWithProcess(checkRouteUrl.Name, "dig", checkRouteUrl.Uri.Host, token);

    private async Task<CheckRouteResult> CheckWithProcess(
        string name,
        string processName,
        string arguments,
        CancellationToken token
    )
    {
        var checkRouteResult = new CheckRouteResult(
            name,
            arguments,
            processName,
            null,
            string.Empty,
            TimeSpan.Zero,
            null
        );
        var stopwatch = new Stopwatch();

        try
        {
            logger.Debug("Start checking {ProcessName} for {Url}", processName, arguments);

            var processTask = processRunner.RunProcess(processName, arguments);
            var waitedTask = await Task.WhenAny(processTask, GetCancellationTask(token));
            var processOutput = waitedTask == processTask ? processTask.Result : null;

            checkRouteResult = checkRouteResult with
            {
                ResponseResult = $"{processOutput}",
                Elapsed = stopwatch.Elapsed,
            };
        }
        catch (Exception ex)
        {
            checkRouteResult = GetCheckRouteResultWithException(checkRouteResult, ex, stopwatch);
        }

        stopwatch.Stop();
        logger.Debug(
            checkRouteResult.Exception,
            "Completed checking {ProcessName} for {Url} with result {Result}",
            processName,
            arguments,
            checkRouteResult.ResponseResult
        );

        return checkRouteResult;
    }

    private static CheckRouteResult GetCheckRouteResultWithException(
        CheckRouteResult checkRouteResult,
        Exception ex,
        Stopwatch stopwatch
    )
    {
        return checkRouteResult with
        {
            ResponseResult =
                $"\"{ex.Message}\" {(ex.InnerException?.Message != null && ex.InnerException?.Message != ex.Message ? $"\"{ex.InnerException?.Message}\"" : null)}",
            Elapsed = stopwatch.Elapsed,
            Exception = ex.InnerException ?? ex,
        };
    }

    private static Task<string> GetCancellationTask(CancellationToken token)
    {
        var tcs = new TaskCompletionSource<string>();
        token.Register(() => tcs.TrySetCanceled());
        return tcs.Task;
    }
}
