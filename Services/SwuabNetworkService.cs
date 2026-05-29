using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using PCOptimizer.Models;
using PCOptimizer.ViewModels;

namespace PCOptimizer.Services;

public sealed class SwuabNetworkService
{
    private readonly AppLogger _logger;
    private readonly WindowsCommandService _commandService;

    public SwuabNetworkService(AppLogger logger, WindowsCommandService commandService)
    {
        _logger = logger;
        _commandService = commandService;
    }

    public bool IsWindows => OperatingSystem.IsWindows();

    public bool IsAdministrator
    {
        get
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            return IsAdministratorCore();
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool IsAdministratorCore()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public async Task<SwuabApplyResult> ApplyAsync(SwuabSettingViewModel setting)
    {
        var validation = Validate(setting);
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (!IsWindows)
        {
            return Unsupported("This TCP/IP setting is only available on Windows.");
        }

        if (!IsAdministrator)
        {
            return new SwuabApplyResult(
                false,
                true,
                true,
                "Requires admin. Restart Mist as administrator, then apply again.",
                string.Empty);
        }

        return setting.Key switch
        {
            "MTU" => await SetMtuAsync((int)setting.NumericValue),
            "AutoTuningLevel" => await NetshTcpGlobalAsync("autotuninglevel", MapAutoTuning(setting.SelectedOption)),
            "InitialRTO" => await SetTcpTemplateValueAsync("InitialRtoMs", ((int)setting.NumericValue).ToString(CultureInfo.InvariantCulture)),
            "MinimumRTO" => await SetTcpTemplateValueAsync("MinRtoMs", ((int)setting.NumericValue).ToString(CultureInfo.InvariantCulture)),
            "ECNCapability" => await NetshTcpGlobalAsync("ecncapability", Enabled(setting.BoolValue)),
            "TCPTimestamps" => await NetshTcpGlobalAsync("timestamps", Enabled(setting.BoolValue)),
            "TCPFastOpen" => await NetshTcpGlobalAsync("fastopen", Enabled(setting.BoolValue)),
            "TCPChimneyOffload" => await NetshTcpGlobalAsync("chimney", Enabled(setting.BoolValue)),
            "MaxSYNRetransmissions" => await SetTcpTemplateValueAsync("MaxSynRetransmissions", ((int)setting.NumericValue).ToString(CultureInfo.InvariantCulture)),
            "ReceiveSideScaling" => await NetshTcpGlobalAsync("rss", Enabled(setting.BoolValue)),
            "DelayedACKFrequency" => await SetActiveInterfaceRegistryValueAsync("TcpAckFrequency", (int)setting.NumericValue),
            "TCPMaxDataRetransmissions" => await SetTcpParametersRegistryValueAsync("TcpMaxDataRetransmissions", (int)setting.NumericValue),
            "TCPTimedWaitDelay" => await SetTcpParametersRegistryValueAsync("TcpTimedWaitDelay", (int)setting.NumericValue),
            "DCA" => await NetshTcpGlobalAsync("dca", Enabled(setting.BoolValue)),
            "NonSACKRTTResiliency" => await SetTcpTemplateValueAsync("NonSackRttResiliency", Enabled(setting.BoolValue)),
            "CongestionProvider" => await SetCongestionProviderAsync(setting.SelectedOption),
            "DefaultHopLimit" => await SetTcpParametersRegistryValueAsync("DefaultTTL", (int)setting.NumericValue),
            _ => Unsupported("No Windows implementation exists for this setting.")
        };
    }

    public async Task<string> ReadCurrentValueAsync(SwuabSettingViewModel setting)
    {
        if (!IsWindows)
        {
            return "Unavailable: Windows only";
        }

        return setting.Key switch
        {
            "MTU" => await ReadActiveMtuAsync(),
            "AutoTuningLevel" => await ReadNetshGlobalValueAsync("Receive Window Auto-Tuning Level"),
            "InitialRTO" => await ReadTcpTemplateValueAsync("InitialRtoMs"),
            "MinimumRTO" => await ReadTcpTemplateValueAsync("MinRtoMs"),
            "ECNCapability" => await ReadNetshGlobalValueAsync("ECN Capability"),
            "TCPTimestamps" => await ReadNetshGlobalValueAsync("RFC 1323 Timestamps"),
            "TCPFastOpen" => await ReadNetshGlobalValueAsync("Fast Open"),
            "TCPChimneyOffload" => await ReadNetshGlobalValueAsync("Chimney Offload State"),
            "MaxSYNRetransmissions" => await ReadTcpTemplateValueAsync("MaxSynRetransmissions"),
            "ReceiveSideScaling" => await ReadNetshGlobalValueAsync("Receive-Side Scaling State"),
            "DelayedACKFrequency" => await ReadActiveInterfaceRegistryValueAsync("TcpAckFrequency"),
            "TCPMaxDataRetransmissions" => await ReadTcpParametersRegistryValueAsync("TcpMaxDataRetransmissions"),
            "TCPTimedWaitDelay" => await ReadTcpParametersRegistryValueAsync("TcpTimedWaitDelay"),
            "DCA" => await ReadNetshGlobalValueAsync("Direct Cache Access"),
            "NonSACKRTTResiliency" => await ReadTcpTemplateValueAsync("NonSackRttResiliency"),
            "CongestionProvider" => await ReadTcpTemplateValueAsync("CongestionProvider"),
            "DefaultHopLimit" => await ReadTcpParametersRegistryValueAsync("DefaultTTL"),
            _ => "Unavailable"
        };
    }

    private SwuabApplyResult Validate(SwuabSettingViewModel setting)
    {
        if (setting.IsSlider)
        {
            var value = (int)setting.NumericValue;
            if (value < setting.Minimum || value > setting.Maximum)
            {
                return new SwuabApplyResult(
                    false,
                    false,
                    true,
                    $"Invalid value. Use {setting.Minimum} to {setting.Maximum}.",
                    string.Empty);
            }
        }

        if (setting.Key == "CongestionProvider" &&
            (setting.SelectedOption.Equals("None", StringComparison.OrdinalIgnoreCase) ||
             setting.SelectedOption.Equals("NewReno", StringComparison.OrdinalIgnoreCase)))
        {
            return Unsupported($"{setting.SelectedOption} is not exposed as a supported Windows congestion provider.");
        }

        return new SwuabApplyResult(true, false, true, "Validated", string.Empty);
    }

    private async Task<SwuabApplyResult> SetMtuAsync(int mtu)
    {
        var indexResult = await ExecutePowerShellAsync(ActiveInterfaceIndexScript + "$index", "Get active IPv4 interface");
        if (indexResult.ExitCode != 0 || string.IsNullOrWhiteSpace(indexResult.Output))
        {
            return Failed("No active IPv4 adapter was found.", indexResult.CommandText, indexResult.Error);
        }

        var interfaceIndex = indexResult.Output.Trim().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
        if (!int.TryParse(interfaceIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return Failed("Could not identify the active IPv4 adapter.", indexResult.CommandText, indexResult.Output);
        }

        var script = $"Set-NetIPInterface -InterfaceIndex {interfaceIndex} -AddressFamily IPv4 -NlMtuBytes {mtu} -ErrorAction Stop";
        return await RunPowerShellApplyAsync(script, $"Set-NetIPInterface -InterfaceIndex {interfaceIndex} -NlMtuBytes {mtu}");
    }

    private async Task<SwuabApplyResult> NetshTcpGlobalAsync(string name, string value)
    {
        var arguments = $"interface tcp set global {name}={value}";
        return await RunApplyAsync("netsh.exe", arguments, $"netsh {arguments}");
    }

    private async Task<SwuabApplyResult> SetTcpTemplateValueAsync(string propertyName, string value)
    {
        var script =
            "$settings = @('InternetCustom','DatacenterCustom'); " +
            $"foreach ($setting in $settings) {{ Set-NetTCPSetting -SettingName $setting -{propertyName} {PowerShellLiteral(value)} -ErrorAction Stop }}";

        return await RunPowerShellApplyAsync(script, $"Set-NetTCPSetting InternetCustom/DatacenterCustom -{propertyName} {value}");
    }

    private async Task<SwuabApplyResult> SetCongestionProviderAsync(string provider)
    {
        var value = provider.Equals("Default", StringComparison.OrdinalIgnoreCase)
            ? "default"
            : provider.ToLowerInvariant();

        var script =
            $"netsh interface tcp set supplemental template=internet congestionprovider={value}; " +
            "if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; " +
            $"netsh interface tcp set supplemental template=internetcustom congestionprovider={value}; " +
            "if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }";

        return await RunPowerShellApplyAsync(script, $"netsh interface tcp set supplemental congestionprovider={value}");
    }

    private async Task<SwuabApplyResult> SetTcpParametersRegistryValueAsync(string name, int value)
    {
        var path = @"HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
        var script = $"New-ItemProperty -Path '{path}' -Name '{name}' -PropertyType DWord -Value {value} -Force -ErrorAction Stop";
        return await RunPowerShellApplyAsync(script, $"Set-ItemProperty {path}\\{name}={value}");
    }

    private async Task<SwuabApplyResult> SetActiveInterfaceRegistryValueAsync(string name, int value)
    {
        var script =
            ActiveInterfaceRegistryPathScript +
            $"New-ItemProperty -Path $path -Name '{name}' -PropertyType DWord -Value {value} -Force -ErrorAction Stop";

        return await RunPowerShellApplyAsync(script, $"Set-ItemProperty active-interface\\{name}={value}");
    }

    private async Task<string> ReadActiveMtuAsync()
    {
        var script = ActiveInterfaceIndexScript + "Get-NetIPInterface -InterfaceIndex $index -AddressFamily IPv4 | Select-Object -ExpandProperty NlMtuBytes";
        return await ReadPowerShellValueAsync(script);
    }

    private async Task<string> ReadTcpTemplateValueAsync(string propertyName)
    {
        var script = $"(Get-NetTCPSetting -SettingName InternetCustom -ErrorAction Stop).{propertyName}";
        return await ReadPowerShellValueAsync(script);
    }

    private async Task<string> ReadTcpParametersRegistryValueAsync(string name)
    {
        var path = @"HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
        var script = $"$value = Get-ItemPropertyValue -Path '{path}' -Name '{name}' -ErrorAction SilentlyContinue; if ($null -eq $value) {{ 'Windows default' }} else {{ $value }}";
        return await ReadPowerShellValueAsync(script);
    }

    private async Task<string> ReadActiveInterfaceRegistryValueAsync(string name)
    {
        var script =
            ActiveInterfaceRegistryPathScript +
            $"$value = Get-ItemPropertyValue -Path $path -Name '{name}' -ErrorAction SilentlyContinue; if ($null -eq $value) {{ 'Windows default' }} else {{ $value }}";

        return await ReadPowerShellValueAsync(script);
    }

    private async Task<string> ReadNetshGlobalValueAsync(string label)
    {
        var result = await _commandService.ExecuteAsync("netsh.exe", "interface tcp show global", explicitlyAllowed: true);
        if (result.ExitCode != 0)
        {
            return $"Unavailable: {Trim(result.Error)}";
        }

        foreach (var line in result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains(label, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(':', 2);
            return parts.Length == 2 ? parts[1].Trim() : line.Trim();
        }

        return "Unavailable on this Windows build";
    }

    private async Task<string> ReadPowerShellValueAsync(string script)
    {
        var result = await ExecutePowerShellAsync(script, "Read Swuab value");
        if (result.ExitCode != 0)
        {
            return $"Unavailable: {Trim(result.Error)}";
        }

        var value = Trim(result.Output);
        return string.IsNullOrWhiteSpace(value) ? "Unavailable" : value;
    }

    private async Task<SwuabApplyResult> RunPowerShellApplyAsync(string script, string displayCommand)
    {
        var result = await ExecutePowerShellAsync(script, displayCommand);
        return ToApplyResult(result, displayCommand);
    }

    private async Task<SwuabApplyResult> RunApplyAsync(string fileName, string arguments, string displayCommand)
    {
        _logger.Info($"Swuab applying: {displayCommand}");
        var result = await _commandService.ExecuteAsync(fileName, arguments, explicitlyAllowed: true);
        return ToApplyResult((result.ExitCode, result.Output, result.Error, displayCommand), displayCommand);
    }

    private async Task<(int ExitCode, string Output, string Error, string CommandText)> ExecutePowerShellAsync(string script, string displayCommand)
    {
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}";
        _logger.Info($"Swuab command: {displayCommand}");
        var result = await _commandService.ExecuteAsync("powershell.exe", arguments, explicitlyAllowed: true);
        return (result.ExitCode, result.Output, result.Error, displayCommand);
    }

    private SwuabApplyResult ToApplyResult((int ExitCode, string Output, string Error, string CommandText) result, string displayCommand)
    {
        var output = Trim(result.Output);
        var error = Trim(result.Error);

        if (result.ExitCode == 0)
        {
            return new SwuabApplyResult(true, false, true, "Applied successfully.", displayCommand);
        }

        var message = string.IsNullOrWhiteSpace(error) ? output : error;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"Command exited with code {result.ExitCode}.";
        }

        return Failed(message, displayCommand, output);
    }

    private static SwuabApplyResult Unsupported(string message) =>
        new(false, false, false, $"Not supported: {message}", string.Empty);

    private static SwuabApplyResult Failed(string message, string commandText, string details) =>
        new(false, false, true, string.IsNullOrWhiteSpace(details) ? message : $"{message} {details}", commandText);

    private static string Enabled(bool value) => value ? "enabled" : "disabled";

    private static string MapAutoTuning(string option) => option switch
    {
        "Highly Restricted" => "highlyrestricted",
        _ => option.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant()
    };

    private static string PowerShellLiteral(string value) =>
        bool.TryParse(value, out _) || int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            ? value
            : $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    private static string Trim(string value) => value.Trim().Replace("\r", string.Empty, StringComparison.Ordinal);

    private const string ActiveInterfaceIndexScript =
        "$index = Get-NetIPInterface -AddressFamily IPv4 | " +
        "Where-Object { $_.ConnectionState -eq 'Connected' -and $_.InterfaceAlias -notmatch 'Loopback' } | " +
        "Sort-Object InterfaceMetric | Select-Object -First 1 -ExpandProperty InterfaceIndex; " +
        "if ($null -eq $index) { throw 'No active IPv4 interface found.' }; ";

    private const string ActiveInterfaceRegistryPathScript =
        ActiveInterfaceIndexScript +
        "$guid = (Get-NetAdapter -InterfaceIndex $index -ErrorAction Stop).InterfaceGuid; " +
        "$path = \"HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\{$guid}\"; ";
}
