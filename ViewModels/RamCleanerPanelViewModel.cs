using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PCOptimizer.Models;
using PCOptimizer.Services;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class RamCleanerPanelViewModel : ViewModelBase
{
    private readonly RamCleanerService _ramCleanerService;
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private bool _allowClosingSelectedNonCriticalApps;
    private bool _focusModeEnabled;
    private double _progress;
    private string _statusText = "Ready";
    private MemoryStats _memoryStats = new();
    private string _processCount = "Processes: Unknown";
    private string _ramCleanedDisplay = "0 MB cleaned";
    private string _cleanResultMessage = "Windows manages RAM automatically. This tool uses safe preview behavior and does not force-close apps.";

    public RamCleanerPanelViewModel(
        RamCleanerService ramCleanerService,
        AppSettings settings,
        SettingsService settingsService)
    {
        _ramCleanerService = ramCleanerService;
        _settings = settings;
        _settingsService = settingsService;

        ProtectedExamples = string.Join(", ", _settings.ProtectedProcesses.Take(8));
        RefreshStats(updateStatusText: true);
        ScanCommand = new AsyncRelayCommand(_ => ScanAsync());
        PreviewCommand = new AsyncRelayCommand(_ => PreviewAsync(), _ => Processes.Count > 0);
        CleanCommand = new AsyncRelayCommand(_ => CleanAsync(), _ => Processes.Count > 0);
        RefreshStatsCommand = new RelayCommand(_ => RefreshStats(updateStatusText: true));
    }

    public event EventHandler<double>? ProgressChanged;

    public ObservableCollection<ProcessInfo> Processes { get; } = new();
    public ObservableCollection<ProcessInfo> PreviewProcesses { get; } = new();
    public string ProtectedExamples { get; }
    public ICommand ScanCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand CleanCommand { get; }
    public ICommand RefreshStatsCommand { get; }

    public MemoryStats MemoryStats
    {
        get => _memoryStats;
        private set
        {
            if (SetProperty(ref _memoryStats, value))
            {
                OnPropertyChanged(nameof(RamUsageMeterWidth));
            }
        }
    }

    public string ProcessCount
    {
        get => _processCount;
        private set => SetProperty(ref _processCount, value);
    }

    public string RamCleanedDisplay
    {
        get => _ramCleanedDisplay;
        private set => SetProperty(ref _ramCleanedDisplay, value);
    }

    public string CleanResultMessage
    {
        get => _cleanResultMessage;
        private set => SetProperty(ref _cleanResultMessage, value);
    }

    public double RamUsageMeterWidth => Math.Max(0, Math.Min(1, MemoryStats.UsagePercent)) * 220;

    public bool AllowClosingSelectedNonCriticalApps
    {
        get => _allowClosingSelectedNonCriticalApps;
        set => SetProperty(ref _allowClosingSelectedNonCriticalApps, value);
    }

    public bool FocusModeEnabled
    {
        get => _focusModeEnabled;
        set
        {
            if (!SetProperty(ref _focusModeEnabled, value))
            {
                return;
            }

            if (value)
            {
                _ramCleanerService.StartFocusMode();
            }
            else
            {
                _ramCleanerService.StopFocusMode();
            }
        }
    }

    public double Progress
    {
        get => _progress;
        set
        {
            if (SetProperty(ref _progress, value))
            {
                ProgressChanged?.Invoke(this, value);
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private async Task ScanAsync()
    {
        Progress = 0.15;
        StatusText = "Scanning processes";
        Processes.Clear();
        PreviewProcesses.Clear();

        var processes = await _ramCleanerService.ScanAsync(_settings.ProtectedProcesses);

        foreach (var process in processes)
        {
            process.PropertyChanged += OnProcessPropertyChanged;
            Processes.Add(process);
        }

        Progress = 1;
        StatusText = $"Scan complete: {Processes.Count} process(es)";
        RefreshStats(updateStatusText: true);
    }

    private async Task PreviewAsync()
    {
        Progress = 0.2;
        StatusText = "Building safe preview";
        PreviewProcesses.Clear();

        var preview = await _ramCleanerService.PreviewCleanAsync(Processes);

        foreach (var process in preview)
        {
            PreviewProcesses.Add(process);
        }

        Progress = 1;
        StatusText = $"Preview ready: {PreviewProcesses.Count} candidate(s)";
    }

    private async Task CleanAsync()
    {
        var before = _ramCleanerService.GetMemoryStats();
        Progress = 0.3;
        StatusText = "Preview-only cleaning";
        await _ramCleanerService.CleanSelectedAsync(Processes, AllowClosingSelectedNonCriticalApps);
        var after = _ramCleanerService.GetMemoryStats();
        MemoryStats = after;

        var cleanedBytes = Math.Max(0, before.UsedBytes - after.UsedBytes);
        RamCleanedDisplay = cleanedBytes > 0
            ? $"{MemoryStats.FormatBytes(cleanedBytes)} cleaned"
            : "0 MB cleaned";

        CleanResultMessage = cleanedBytes > 0
            ? $"RAM usage dropped by {MemoryStats.FormatBytes(cleanedBytes)}."
            : "No meaningful RAM freed. No apps were closed automatically.";

        Progress = 1;
        StatusText = "Finished safely. No apps were closed.";
        ProcessCount = _ramCleanerService.GetProcessCountDisplay();
    }

    public void RefreshLiveStats()
    {
        RefreshStats(updateStatusText: false);
    }

    private void RefreshStats(bool updateStatusText)
    {
        MemoryStats = _ramCleanerService.GetMemoryStats();
        ProcessCount = _ramCleanerService.GetProcessCountDisplay();

        if (updateStatusText)
        {
            StatusText = $"RAM refreshed: {MemoryStats.UsagePercentDisplay} used";
        }
    }

    private void OnProcessPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ProcessInfo process || e.PropertyName != nameof(ProcessInfo.IsProtected))
        {
            return;
        }

        if (process.IsProtected && !_settings.ProtectedProcesses.Contains(process.Name))
        {
            _settings.ProtectedProcesses.Add(process.Name);
            _settingsService.Save(_settings);
        }
        else if (!process.IsProtected)
        {
            _settings.ProtectedProcesses.RemoveAll(name => string.Equals(name, process.Name, StringComparison.OrdinalIgnoreCase));
            _settingsService.Save(_settings);
        }
    }
}
