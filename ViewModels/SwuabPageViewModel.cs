using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PCOptimizer.Models;
using PCOptimizer.Services;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class SwuabPageViewModel : ViewModelBase
{
    private const string ConfigVersion = "1";
    private readonly SwuabNetworkService _networkService;
    private readonly AppLogger _logger;
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private double _progress;
    private string _statusText = "Ready. Review settings, then click Apply selected.";
    private bool _isBusy;

    public SwuabPageViewModel(
        SwuabNetworkService networkService,
        AppLogger logger,
        AppSettings settings,
        SettingsService settingsService)
    {
        _networkService = networkService;
        _logger = logger;
        _settings = settings;
        _settingsService = settingsService;

        Settings = new ObservableCollection<SwuabSettingViewModel>(CreateSettings());
        TcpSettings = new ObservableCollection<SwuabSettingViewModel>(Settings.Where(setting => setting.Section == "TCP Settings"));
        IpSettings = new ObservableCollection<SwuabSettingViewModel>(Settings.Where(setting => setting.Section == "IP Settings"));

        foreach (var setting in Settings)
        {
            if (_settings.SwuabValues.TryGetValue(setting.Key, out var savedValue))
            {
                setting.TrySetFromText(savedValue, out _);
            }

            setting.PropertyChanged += OnSettingPropertyChanged;
        }

        ApplySelectedCommand = new AsyncRelayCommand(_ => ApplySelectedAsync(), _ => !IsBusy);
        ResetDefaultsCommand = new AsyncRelayCommand(_ => ResetDefaultsAsync(), _ => !IsBusy);
        RefreshCurrentValuesCommand = new AsyncRelayCommand(_ => RefreshCurrentValuesAsync(), _ => !IsBusy);
        SaveConfigCommand = new RelayCommand(_ => SaveConfig());
        SelectAllCommand = new RelayCommand(_ => SetAllSelected(true));
        DeselectAllCommand = new RelayCommand(_ => SetAllSelected(false));
    }

    public ObservableCollection<SwuabSettingViewModel> Settings { get; }
    public ObservableCollection<SwuabSettingViewModel> TcpSettings { get; }
    public ObservableCollection<SwuabSettingViewModel> IpSettings { get; }
    public ICommand ApplySelectedCommand { get; }
    public ICommand ResetDefaultsCommand { get; }
    public ICommand RefreshCurrentValuesCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }

    public string AdminStateText => _networkService.IsWindows
        ? _networkService.IsAdministrator
            ? "Admin available"
            : "Admin required to apply changes"
        : "Windows-only page";

    public string SelectedCountDisplay => $"{Settings.Count(setting => setting.IsSelected)} selected";

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public async Task LoadConfigAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            StatusText = "Load failed: the selected file does not exist.";
            return;
        }

        Dictionary<string, string> values;
        try
        {
            values = ParseConfig(await File.ReadAllLinesAsync(path));
        }
        catch (Exception ex)
        {
            StatusText = $"Load failed: {ex.Message}";
            _logger.Warning($"Swuab config load failed: {ex.Message}");
            return;
        }

        var errors = ValidateConfigValues(values);
        if (errors.Count > 0)
        {
            StatusText = $"Load failed: {errors[0]}";
            _logger.Warning($"Swuab config rejected: {string.Join(" ", errors)}");
            return;
        }

        foreach (var setting in Settings)
        {
            setting.TrySetFromText(values[setting.Key], out _);
            setting.StatusText = "Loaded from config. Not applied yet.";
        }

        SaveCurrentValues();
        StatusText = $"Loaded Swuab config from {path}. Click Apply selected to apply it.";
        _logger.Info($"Swuab config loaded from {path}.");
    }

    private async Task ApplySelectedAsync()
    {
        var selected = Settings.Where(setting => setting.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText = "Apply skipped: no settings selected.";
            return;
        }

        IsBusy = true;
        Progress = 0;
        var succeeded = 0;
        var failed = 0;
        var skipped = 0;

        try
        {
            _logger.Warning("Swuab apply started. TCP/IP changes may affect connectivity; save a config or restore point before major changes.");

            for (var i = 0; i < selected.Count; i++)
            {
                var setting = selected[i];
                setting.StatusText = $"Will change to {setting.ValueDisplay}.";
                StatusText = $"Applying {setting.Title} ({i + 1}/{selected.Count})";
                _logger.Info($"Swuab {setting.Title}: applying value {setting.ExportValue}.");

                var result = await _networkService.ApplyAsync(setting);
                setting.StatusText = result.Message;

                if (!string.IsNullOrWhiteSpace(result.CommandText))
                {
                    _logger.Info($"Swuab {setting.Title}: {result.CommandText}");
                }

                if (result.Succeeded)
                {
                    succeeded++;
                }
                else if (result.RequiresAdmin || !result.IsSupported)
                {
                    skipped++;
                    _logger.Warning($"Swuab {setting.Title}: {result.Message}");
                }
                else
                {
                    failed++;
                    _logger.Error($"Swuab {setting.Title}: {result.Message}");
                }

                Progress = (i + 1d) / selected.Count;
            }

            SaveCurrentValues();
            StatusText = $"Apply complete: {succeeded} succeeded, {skipped} skipped, {failed} failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResetDefaultsAsync()
    {
        IsBusy = true;
        Progress = 0;

        try
        {
            foreach (var setting in Settings)
            {
                setting.TrySetFromText(DefaultValues[setting.Key], out _);
                setting.IsSelected = true;
                setting.StatusText = "Windows default queued.";
            }

            SaveCurrentValues();
            StatusText = "Windows defaults queued. Applying defaults now.";
            await ApplySelectedAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshCurrentValuesAsync()
    {
        IsBusy = true;
        Progress = 0;

        try
        {
            for (var i = 0; i < Settings.Count; i++)
            {
                var setting = Settings[i];
                StatusText = $"Refreshing {setting.Title} ({i + 1}/{Settings.Count})";
                setting.CurrentWindowsValue = await _networkService.ReadCurrentValueAsync(setting);
                setting.StatusText = "Current value refreshed.";
                Progress = (i + 1d) / Settings.Count;
            }

            StatusText = "Current Windows values refreshed where available.";
            _logger.Info("Swuab current values refreshed.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SaveConfig()
    {
        try
        {
            var downloads = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            Directory.CreateDirectory(downloads);
            var path = Path.Combine(downloads, $"Mist-Swuab-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(path, BuildConfigText());
            StatusText = $"Saved Swuab config to {path}";
            _logger.Info($"Swuab config saved to {path}.");
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
            _logger.Error($"Swuab config save failed: {ex.Message}");
        }
    }

    private string BuildConfigText()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"MistConfigVersion={ConfigVersion}");
        builder.AppendLine("Page=Swuab");

        foreach (var setting in Settings)
        {
            builder.AppendLine($"{setting.Key}={setting.ExportValue}");
        }

        return builder.ToString();
    }

    private Dictionary<string, string> ParseConfig(IEnumerable<string> lines)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split('=', 2);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                throw new InvalidDataException($"Invalid line: {line}");
            }

            values[parts[0].Trim()] = parts[1].Trim();
        }

        if (!values.TryGetValue("MistConfigVersion", out var version) || version != ConfigVersion)
        {
            throw new InvalidDataException("unsupported or missing MistConfigVersion.");
        }

        if (!values.TryGetValue("Page", out var page) || !page.Equals("Swuab", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("this file is not a Swuab config.");
        }

        values.Remove("MistConfigVersion");
        values.Remove("Page");
        return values;
    }

    private List<string> ValidateConfigValues(Dictionary<string, string> values)
    {
        var errors = new List<string>();
        var expectedKeys = Settings.Select(setting => setting.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var key in expectedKeys)
        {
            if (!values.ContainsKey(key))
            {
                errors.Add($"Missing {key}.");
            }
        }

        foreach (var key in values.Keys)
        {
            if (!expectedKeys.Contains(key))
            {
                errors.Add($"Unknown setting {key}.");
            }
        }

        foreach (var setting in Settings)
        {
            if (!values.TryGetValue(setting.Key, out var value))
            {
                continue;
            }

            var probe = CreateSetting(setting.Key);
            if (!probe.TrySetFromText(value, out var error))
            {
                errors.Add(error);
            }
        }

        return errors;
    }

    private void SetAllSelected(bool selected)
    {
        foreach (var setting in Settings)
        {
            setting.IsSelected = selected;
        }

        StatusText = selected ? "All Swuab settings selected." : "All Swuab settings deselected.";
        OnPropertyChanged(nameof(SelectedCountDisplay));
    }

    private void OnSettingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not SwuabSettingViewModel setting)
        {
            return;
        }

        if (e.PropertyName == nameof(SwuabSettingViewModel.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedCountDisplay));
            return;
        }

        if (e.PropertyName is nameof(SwuabSettingViewModel.NumericValue) or
            nameof(SwuabSettingViewModel.BoolValue) or
            nameof(SwuabSettingViewModel.SelectedOption))
        {
            _settings.SwuabValues[setting.Key] = setting.ExportValue;
            _settingsService.Save(_settings);
        }
    }

    private void SaveCurrentValues()
    {
        foreach (var setting in Settings)
        {
            _settings.SwuabValues[setting.Key] = setting.ExportValue;
        }

        _settingsService.Save(_settings);
    }

    private static IEnumerable<SwuabSettingViewModel> CreateSettings() => SettingOrder.Select(CreateSetting);

    private static SwuabSettingViewModel CreateSetting(string key) => key switch
    {
        "MTU" => new SwuabSettingViewModel(
            key,
            "MTU Value",
            "TCP Settings",
            "Maximum Transmission Unit determines the largest packet size. Higher values optimize throughput on stable connections, while lower values reduce fragmentation on congested networks. Affects overall network efficiency and latency.",
            SwuabSettingKind.Slider,
            numericValue: 1500,
            minimum: 576,
            maximum: 1500),
        "AutoTuningLevel" => new SwuabSettingViewModel(
            key,
            "Auto-Tuning Level",
            "TCP Settings",
            "Controls TCP receive window auto-tuning mechanism. Normal provides balanced optimization, Restricted reduces memory usage, Highly Restricted minimizes latency impact, Disabled allows manual control, and Experimental maximizes throughput potential.",
            SwuabSettingKind.Dropdown,
            selectedOption: "Normal",
            options: ["Normal", "Restricted", "Highly Restricted", "Disabled", "Experimental"]),
        "InitialRTO" => new SwuabSettingViewModel(
            key,
            "Initial RTO",
            "TCP Settings",
            "Initial Retransmission Timeout in milliseconds for packet retry. Lower values improve packet loss recovery speed but may cause unnecessary retransmissions. Higher values reduce network load at cost of recovery time.",
            SwuabSettingKind.Slider,
            numericValue: 3000,
            minimum: 300,
            maximum: 3000),
        "MinimumRTO" => new SwuabSettingViewModel(
            key,
            "Minimum RTO",
            "TCP Settings",
            "Minimum Retransmission Timeout in milliseconds.",
            SwuabSettingKind.Slider,
            numericValue: 300,
            minimum: 20,
            maximum: 300),
        "ECNCapability" => Toggle(key, "ECN Capability", "Explicit Congestion Notification for better congestion control.", false),
        "TCPTimestamps" => Toggle(key, "TCP Timestamps", "Enables precise round-trip time measurement. Improves congestion control accuracy but adds 12 bytes overhead per packet. Useful for monitoring connection quality. Consider disabling for minimal latency requirements.", false),
        "TCPFastOpen" => Toggle(key, "TCP Fast Open", "Enables data transmission during initial TCP handshake, reducing connection setup time by one round-trip. Improves application responsiveness and loading times. Minimal security impact for gaming applications.", false),
        "TCPChimneyOffload" => Toggle(key, "TCP Chimney Offload", "Offloads TCP processing to network hardware. Reduces CPU load but may increase latency variance.", false),
        "MaxSYNRetransmissions" => new SwuabSettingViewModel(
            key,
            "Max SYN Retransmissions",
            "TCP Settings",
            "Controls connection attempt retry count. Lower values reduce connection timeout delays but may affect reliability. Higher values improve connection success rate but increase waiting time during failures.",
            SwuabSettingKind.Slider,
            numericValue: 2,
            minimum: 2,
            maximum: 4),
        "ReceiveSideScaling" => Toggle(key, "Receive Side Scaling", "Distributes network processing across CPU cores. Significantly improves performance on multi-core systems. Essential for high-bandwidth, low-latency applications. Disable only if experiencing CPU-related issues.", true),
        "DelayedACKFrequency" => new SwuabSettingViewModel(
            key,
            "Delayed ACK Frequency",
            "TCP Settings",
            "Sets the number of packets to wait before sending acknowledgments.",
            SwuabSettingKind.Slider,
            numericValue: 2,
            minimum: 1,
            maximum: 10),
        "TCPMaxDataRetransmissions" => new SwuabSettingViewModel(
            key,
            "TCP Max Data Retransmissions",
            "TCP Settings",
            "Controls connection attempt retry count. Lower values reduce connection timeout delays but may affect reliability. Higher values improve connection success rate but increase waiting time during failures.",
            SwuabSettingKind.Slider,
            numericValue: 5,
            minimum: 0,
            maximum: 10),
        "TCPTimedWaitDelay" => new SwuabSettingViewModel(
            key,
            "TCP Timed Wait Delay",
            "TCP Settings",
            "Sets the number of seconds to wait before sending acknowledgments.",
            SwuabSettingKind.Slider,
            numericValue: 240,
            minimum: 30,
            maximum: 300),
        "DCA" => Toggle(key, "DCA", "Direct Cache Access", false),
        "NonSACKRTTResiliency" => Toggle(key, "Non-SACK RTT Resiliency", "Enables the use of Non-SACK RTT resiliency.", true),
        "CongestionProvider" => new SwuabSettingViewModel(
            key,
            "Congestion Provider",
            "TCP Settings",
            "Sets TCP congestion control algorithm. CTCP provides balanced throughput and latency for mixed usage. DCTCP optimizes for low-latency networks. None disables congestion control for minimal latency at stability cost.",
            SwuabSettingKind.Dropdown,
            selectedOption: "Default",
            options: ["CTCP", "NewReno", "None", "DCTCP", "Default"]),
        "DefaultHopLimit" => new SwuabSettingViewModel(
            key,
            "Default Hop Limit",
            "IP Settings",
            "Sets the default Hop Limit for IPv4 packets.",
            SwuabSettingKind.Slider,
            numericValue: 128,
            minimum: 0,
            maximum: 255),
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown Swuab setting.")
    };

    private static SwuabSettingViewModel Toggle(string key, string title, string description, bool defaultValue) =>
        new(key, title, "TCP Settings", description, SwuabSettingKind.Toggle, boolValue: defaultValue);

    private static readonly string[] SettingOrder =
    [
        "MTU",
        "AutoTuningLevel",
        "InitialRTO",
        "MinimumRTO",
        "ECNCapability",
        "TCPTimestamps",
        "TCPFastOpen",
        "TCPChimneyOffload",
        "MaxSYNRetransmissions",
        "ReceiveSideScaling",
        "DelayedACKFrequency",
        "TCPMaxDataRetransmissions",
        "TCPTimedWaitDelay",
        "DCA",
        "NonSACKRTTResiliency",
        "CongestionProvider",
        "DefaultHopLimit"
    ];

    private static readonly Dictionary<string, string> DefaultValues = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MTU"] = "1500",
        ["AutoTuningLevel"] = "Normal",
        ["InitialRTO"] = "3000",
        ["MinimumRTO"] = "300",
        ["ECNCapability"] = "false",
        ["TCPTimestamps"] = "false",
        ["TCPFastOpen"] = "false",
        ["TCPChimneyOffload"] = "false",
        ["MaxSYNRetransmissions"] = "2",
        ["ReceiveSideScaling"] = "true",
        ["DelayedACKFrequency"] = "2",
        ["TCPMaxDataRetransmissions"] = "5",
        ["TCPTimedWaitDelay"] = "240",
        ["DCA"] = "false",
        ["NonSACKRTTResiliency"] = "true",
        ["CongestionProvider"] = "Default",
        ["DefaultHopLimit"] = "128"
    };
}
