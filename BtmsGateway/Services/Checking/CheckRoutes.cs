using System.Diagnostics;
using BtmsGateway.Utils.Http;
using ILogger = Serilog.ILogger;

namespace BtmsGateway.Services.Checking;

public class CheckRoutes(HealthCheckConfig healthCheckConfig, IHttpClientFactory clientFactory, ILogger logger)
{
    public const int OverallTimeoutSecs = 50;
    
    public async Task<IEnumerable<CheckRouteResult>> Check()
    {
        if (healthCheckConfig.Disabled) return [];
        
        logger.Information("Start route checking");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(OverallTimeoutSecs));
        var checkRouteResults = await Task.WhenAll(healthCheckConfig.Urls.Select(x => new CheckRouteUrl { Name = x.Key, Method = x.Value.Method, Disabled = x.Value.Disabled, Url = x.Value.Url, CheckType = "HTTP" })
                                                                         .Select(x => Check(x, cts)));
        return checkRouteResults.SelectMany(routeResults => routeResults);
    }
    
    private async Task<IEnumerable<CheckRouteResult>> Check(CheckRouteUrl checkRouteUrl, CancellationTokenSource cts)
    {
        if (checkRouteUrl.Disabled)
            return [new CheckRouteResult(checkRouteUrl.Name, $"{checkRouteUrl.Method} {checkRouteUrl.Url}", string.Empty, "Disabled", TimeSpan.Zero)];
        
        var checks = new List<Task<CheckRouteResult>> { CheckHttp(checkRouteUrl, cts) };
        if (checkRouteUrl.Uri.PathAndQuery != "/") checks.Add(CheckHttp(checkRouteUrl with { CheckType = "HTTP HOST", Url = checkRouteUrl.Url.Replace(checkRouteUrl.Uri.PathAndQuery, "")}, cts));
        
        return await Task.WhenAll(checks);
    }
    
    private async Task<CheckRouteResult> CheckHttp(CheckRouteUrl checkRouteUrl, CancellationTokenSource cts)
    {
        var checkRouteResult = new CheckRouteResult(checkRouteUrl.Name, $"{checkRouteUrl.Method} {checkRouteUrl.Url}", checkRouteUrl.CheckType, string.Empty, TimeSpan.Zero);
        var stopwatch = new Stopwatch();
    
        try
        {
            logger.Information("Start checking HTTP request for {Url}", checkRouteUrl.Url);
            var client = clientFactory.CreateClient(Proxy.ProxyClientWithoutRetry);
            var request = new HttpRequestMessage(new HttpMethod(checkRouteUrl.Method), checkRouteUrl.Url);
            stopwatch.Start();
            var response = await client.SendAsync(request, cts.Token);
            checkRouteResult = checkRouteResult with { ResponseResult = $"{response.StatusCode.ToString()} ({(int)response.StatusCode})", Elapsed = stopwatch.Elapsed };
        }
        catch (Exception ex)
        {
            checkRouteResult = checkRouteResult with { ResponseResult = $"\"{ex.Message}\"", Elapsed = stopwatch.Elapsed };
        }
        
        stopwatch.Stop();
        logger.Information("Completed checking HTTP request for {Url} with result {Result}", checkRouteUrl.Url, checkRouteResult.ResponseResult);
        
        return checkRouteResult;
    }
    
    private Task<CheckRouteResult> CheckNsLookup(CheckRouteUrl checkRouteUrl, CancellationTokenSource cts)
    {
        var checkRouteResult = new CheckRouteResult(checkRouteUrl.Name, checkRouteUrl.Uri.Host, "nslookup", string.Empty, TimeSpan.Zero);
        var stopwatch = new Stopwatch();
    
        try
        {
            logger.Information("Start checking nslookup for {Url}", checkRouteUrl.Url);
    
            var processOutput = RunProcess("nslookup", checkRouteUrl.Uri.Host);
            checkRouteResult = checkRouteResult with { ResponseResult = $"{processOutput}", Elapsed = stopwatch.Elapsed };
        }
        catch (Exception ex)
        {
            checkRouteResult = checkRouteResult with { ResponseResult = $"\"{ex.Message}\"", Elapsed = stopwatch.Elapsed };
        }
        
        stopwatch.Stop();
        logger.Information("Completed checking nslookup for {Url} with result {Result}", checkRouteUrl.Url, checkRouteResult.ResponseResult);
        
        return Task.FromResult(checkRouteResult);
    }
    
    private static string RunProcess(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
            
        using var process = Process.Start(startInfo);
        using var outputReader = process?.StandardOutput;
        //using var errorReader = process?.StandardError;
        var readToEnd = outputReader?.ReadToEnd();
        return $"{readToEnd}".Replace("\r\n", "\n").Replace("\n\n", "\n").Trim(' ', '\n');
    }
}
