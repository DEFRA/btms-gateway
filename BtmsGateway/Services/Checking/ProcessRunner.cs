using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BtmsGateway.Services.Checking;

public interface IProcessRunner
{
    Task<string> RunProcess(string fileName, string arguments);
}

[ExcludeFromCodeCoverage]
public class ProcessRunner : IProcessRunner
{
    public Task<string> RunProcess(string fileName, string arguments)
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
        var readToEnd = outputReader?.ReadToEnd();
        return Task.FromResult($"{readToEnd}".Replace("\r\n", "\n").Replace("\n\n", "\n").Trim(' ', '\n'));
    }
}