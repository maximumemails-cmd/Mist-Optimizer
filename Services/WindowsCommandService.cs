using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PCOptimizer.Services;

public sealed class WindowsCommandService
{
    private readonly AppLogger _logger;

    public WindowsCommandService(AppLogger logger)
    {
        _logger = logger;
    }

    public async Task<(int ExitCode, string Output, string Error)> ExecuteAsync(
        string fileName,
        string arguments,
        bool explicitlyAllowed)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.Warning("Windows command skipped because this platform is not Windows.");
            return (-1, string.Empty, "Windows-only command.");
        }

        if (!explicitlyAllowed)
        {
            _logger.Warning("Windows command blocked because it was not explicitly allowed by a safe implemented optimisation.");
            return (-1, string.Empty, "Command was not explicitly allowed.");
        }

        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            return (-1, string.Empty, "Process failed to start.");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, output, error);
    }
}
