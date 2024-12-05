using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using BtmsGateway.Services.Routing;
using BtmsGateway.Utils.Http;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Checking;

public class CheckRoutes(IMessageRoutes messageRoutes, IHttpClientFactory clientFactory, ILogger logger)
{
    public const int OverallTimeoutSecs = 50;

    public async Task<IEnumerable<CheckRouteResult>> CheckAll()
    {
        logger.Information("Start route checking");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(OverallTimeoutSecs));
        return (await Task.WhenAll(messageRoutes.HealthUrls.Where(x => !x.Disabled).Select(healthUrl => CheckAll(healthUrl, cts)))).SelectMany(routeResults => routeResults);
    }

    public async Task<IEnumerable<CheckRouteResult>> CheckIpaffs()
    {
        logger.Information("Start route checking");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(OverallTimeoutSecs));
        return (await Task.WhenAll(messageRoutes.HealthUrls.Where(x => !x.Disabled && x.Name.StartsWith("IPAFFS")).Select(healthUrl => CheckIpaffs(healthUrl, cts)))).SelectMany(routeResults => routeResults);
    }

    private async Task<IEnumerable<CheckRouteResult>> CheckAll(HealthUrl healthUrl, CancellationTokenSource cts)
    {
        var checks = new List<Task<CheckRouteResult>>
        {
            CheckHttp(healthUrl, false, cts.Token),
            CheckNsLookup(healthUrl, cts.Token)
        };
        if (healthUrl.Uri.PathAndQuery != "/") checks.Add(CheckHttp(healthUrl with { CheckType = "HTTP HOST", Url = healthUrl.Url.Replace(healthUrl.Uri.PathAndQuery, "")}, false, cts.Token));
        
        return await Task.WhenAll(checks);
    }

    private async Task<IEnumerable<CheckRouteResult>> CheckIpaffs(HealthUrl healthUrl, CancellationTokenSource cts)
    {
        try
        {
            var client = clientFactory.CreateClient(Proxy.ProxyClientWithoutRetry);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://openid-token-microservice.azurewebsites.net/ad/sign");
            request.Content = JsonContent.Create("{\"iss\":\"https://imports-proxy-static.azurewebsites.net\"}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"poc:5295CwcxefGhO3Vrg9C4cGe6ZmuMkWZBdMMQ")));
            var tokenResponse = await client.SendAsync(request, cts.Token);
            var authToken = await tokenResponse.Content.ReadAsStringAsync(cts.Token);
            return [await CheckHttp(healthUrl, true, cts.Token, new AuthenticationHeaderValue("Bearer", authToken))];
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Checking IPAFFS request for {Url} failed", healthUrl.Url);
            return Array.Empty<CheckRouteResult>();
        }
    }

    private async Task<CheckRouteResult> CheckHttp(HealthUrl healthUrl, bool includeResponseBody, CancellationToken token, AuthenticationHeaderValue? authHeader = null)
    {
        var checkRouteResult = new CheckRouteResult(healthUrl.Name, $"{healthUrl.Method} {healthUrl.Url}", healthUrl.CheckType, string.Empty, TimeSpan.Zero);
        var stopwatch = new Stopwatch();

        try
        {
            logger.Information("Start checking HTTP request for {Url}", healthUrl.Url);
            var client = clientFactory.CreateClient(Proxy.ProxyClientWithoutRetry);
            var request = new HttpRequestMessage(new HttpMethod(healthUrl.Method), healthUrl.Url);
            if (authHeader != null) request.Headers.Authorization = authHeader;
            stopwatch.Start();
            var response = await client.SendAsync(request, token);
            checkRouteResult = checkRouteResult with { ResponseResult = $"{response.StatusCode.ToString()} ({(int)response.StatusCode}){(includeResponseBody ? $"\n{await response.Content.ReadAsStringAsync()}" : string.Empty)}", Elapsed = stopwatch.Elapsed };
        }
        catch (Exception ex)
        {
            checkRouteResult = checkRouteResult with { ResponseResult = $"\"{ex.Message}\"", Elapsed = stopwatch.Elapsed };
        }
        
        stopwatch.Stop();
        logger.Information("Completed checking HTTP request for {Url} with result {Result}", healthUrl.Url, checkRouteResult.ResponseResult);
        
        return checkRouteResult;
    }

    private async Task<CheckRouteResult> CheckNsLookup(HealthUrl healthUrl, CancellationToken token)
    {
        var checkRouteResult = new CheckRouteResult(healthUrl.Name, healthUrl.Uri.Host, "nslookup", string.Empty, TimeSpan.Zero);
        var stopwatch = new Stopwatch();

        try
        {
            logger.Information("Start checking nslookup for {Url}", healthUrl.Uri.Host);

            var processTask = RunProcess("nslookup", healthUrl.Uri.Host);
            var waitedTask = await Task.WhenAny(processTask, GetCancellationTask(token));
            var processOutput = waitedTask == processTask ? processTask.Result : null;
                
            checkRouteResult = checkRouteResult with { ResponseResult = $"{processOutput}", Elapsed = stopwatch.Elapsed };
        }
        catch (Exception ex)
        {
            checkRouteResult = checkRouteResult with { ResponseResult = $"\"{ex.Message}\"", Elapsed = stopwatch.Elapsed };
        }
        
        stopwatch.Stop();
        logger.Information("Completed checking nslookup for {Url} with result {Result}", healthUrl.Url, checkRouteResult.ResponseResult);
        
        return checkRouteResult;
    }

    private static Task<string> RunProcess(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        using var outputReader = process?.StandardOutput;
        //using var errorReader = process?.StandardError;
        var readToEnd = outputReader?.ReadToEnd();
        return Task.FromResult($"{readToEnd}".Replace("\r\n", "\n").Replace("\n\n", "\n").Trim(' ', '\n'));
    }

    private static Task GetCancellationTask(CancellationToken token)
    {
        var tcs = new TaskCompletionSource<string>();
        token.Register(() => tcs.TrySetCanceled());
        return tcs.Task;
    }
}
