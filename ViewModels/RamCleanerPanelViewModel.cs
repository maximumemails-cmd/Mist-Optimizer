using System;
using System.Collections.Generic;
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
    private readonly List<ProcessInfo> _allProcesses = [];
    private bool _trimSelectedApps = true;
    private bool _closeSelectedApps;
    private bool _focusModeEnabled = true;
    private bool _standbyCleanupEnabled;
    private double _progress;
    private string _statusText = "Ready";
    private MemoryStats _memoryStats = new();
    private string _processCount = "Processes: Unknown";
    private string _ramCleanedDisplay = "0 MB cleaned";
    private string _cleanResultMessage = "Scan processes to see safe recommendations. Mist never force-kills protected Windows processes.";
    private string _searchText = string.Empty;
    private string _previewSummary = "Preview will show selected actions before Mist changes anything.";
    private string _lastCleanDisplay = "Last clean: not run yet";

    public RamCleanerPanelViewModel(
        RamCleanerService ramCleanerService,
        AppSettings settings,
        SettingsService settingsService)
    {
        _ramCleanerService = ramCleanerService;
        _settings = settings;
        _settingsService = settingsService;

        ProtectedExamples = string.Join(", ", _settings.ProtectedProcesses.Take(10));
        StandbyCleanupStatus = _ramCleanerService.StandbyCleanupStatus;
        _focusModeEnabled = _settings.RamCleanerFocusSuggestionsEnabled;
        _trimSelectedApps = _settings.RamCleanerTrimSelectedApps;
        _closeSelectedApps = _settings.RamCleanerCloseSelectedApps;
        _standbyCleanupEnabled = _ramCleanerService.IsStandbyCleanupAvailable && _settings.RamCleanerStandbyCleanupEnabled;

        RefreshStats(updateStatusText: true);
        ScanCommand = new AsyncRelayCommand(_ => ScanAsync());
        PreviewCommand = new AsyncRelayCommand(_ => PreviewAsync(), _ => _allProcesses.Count > 0);
        CleanCommand = new AsyncRelayCommand(_ => CleanAsync());
        RefreshStatsCommand = new RelayCommand(_ => RefreshStats(updateStatusText: true));
        SelectRecommendedCommand = new RelayCommand(_ => SelectRecommended(), _ => _allProcesses.Count > 0);
        DeselectAllCommand = new RelayCommand(_ => DeselectAll(), _ => _allProcesses.Count > 0);

        _ = ScanAsync();
    }

    public event EventHandler<double>? ProgressChanged;

    public ObservableCollection<ProcessInfo> Processes { get; } = new();
    public ObservableCollection<ProcessInfo> PreviewProcesses { get; } = new();
    public ObservableCollection<MemoryGraphPoint> MemoryHistory { get; } = new();
    public string ProtectedExamples { get; }
    public string StandbyCleanupStatus { get; }
    public ICommand ScanCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand CleanCommand { get; }
    public ICommand RefreshStatsCommand { get; }
    public ICommand SelectRecommendedCommand { get; }
    public ICommand DeselectAllCommand { get; }

    public MemoryStats MemoryStats
    {
        get => _memoryStats;
        private set
        {
            if (SetProperty(ref _memoryStats, value))
            {
                OnPropertyChanged(nameof(RamUsageMeterWidth));
                OnPropertyChanged(nameof(MemoryTimestampDisplay));
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

    public string PreviewSummary
    {
        get => _previewSummary;
        private set => SetProperty(ref _previewSummary, value);
    }

    public string LastCleanDisplay
    {
        get => _lastCleanDisplay;
        private set => SetProperty(ref _lastCleanDisplay, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyProcessFilter();
            }
        }
    }

    public double RamUsageMeterWidth => Math.Max(0, Math.Min(1, MemoryStats.UsagePercent)) * 220;
    public string MemoryTimestampDisplay => $"Updated {MemoryStats.TimestampDisplay}";
    public string SelectedItemsDisplay => $"{SelectedProcessCount} selected";
    public string EstimatedSelectedDisplay => $"Estimated reclaim: {MemoryStats.FormatBytes(SelectedEstimatedBytes)}";
    public string RecommendedCountDisplay => $"{_allProcesses.Count(process => process.IsRecommended)} recommended";
    public string VisibleProcessCountDisplay => Processes.Count == _allProcesses.Count
        ? $"{Processes.Count} scanned"
        : $"{Processes.Count} shown of {_allProcesses.Count}";
    public bool HasProcesses => _allProcesses.Count > 0;
    public bool HasVisibleProcesses => Processes.Count > 0;
    public bool IsProcessEmptyVisible => Processes.Count == 0;
    public int SelectedProcessCount => _allProcesses.Count(process => process.IsSelectedForCleaning);
    public long SelectedEstimatedBytes => _allProcesses
        .Where(process => process.IsSelectedForCleaning)
        .Sum(process => process.EstimatedReclaimableBytes);
    public bool CanUseStandbyCleanup => _ramCleanerService.IsStandbyCleanupAvailable;

    public bool TrimSelectedApps
    {
        get => _trimSelectedApps;
        set
        {
            if (SetProperty(ref _trimSelectedApps, value))
            {
                _settings.RamCleanerTrimSelectedApps = value;
                _settingsService.Save(_settings);
            }
        }
    }

    public bool CloseSelectedApps
    {
        get => _closeSelectedApps;
        set
        {
            if (SetProperty(ref _closeSelectedApps, value))
            {
                _settings.RamCleanerCloseSelectedApps = value;
                _settingsService.Save(_settings);
            }
        }
    }

    public bool StandbyCleanupEnabled
    {
        get => _standbyCleanupEnabled;
        set
        {
            if (!CanUseStandbyCleanup)
            {
                value = false;
            }

            if (SetProperty(ref _standbyCleanupEnabled, value))
            {
                _settings.RamCleanerStandbyCleanupEnabled = value;
                _settingsService.Save(_settings);
            }
        }
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

            _settings.RamCleanerFocusSuggestionsEnabled = value;
            _settingsService.Save(_settings);

            if (value)
            {
                _ramCleanerService.StartFocusMode();
            }
            else
            {
                _ramCleanerService.StopFocusMode();
            }

            if (_allProcesses.Count > 0)
            {
                _ = ScanAsync();
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
        StatusText = FocusModeEnabled ? "Scanning with focus suggestions" : "Scanning processes";
        ClearProcessSubscriptions();
        _allProcesses.Clear();
        Processes.Clear();
        PreviewProcesses.Clear();

        var processes = await _ramCleanerService.ScanAsync(_settings.ProtectedProcesses, FocusModeEnabled);

        foreach (var process in processes)
        {
            if (_settings.RamCleanerProcessSelections.TryGetValue(GetProcessSelectionKey(process), out var savedSelection))
            {
                process.IsSelectedForCleaning = savedSelection;
            }

            process.PropertyChanged += OnProcessPropertyChanged;
            _allProcesses.Add(process);
        }

        ApplyProcessFilter();
        Progress = 1;
        StatusText = $"Scan complete: {_allProcesses.Count} process(es), {_allProcesses.Count(process => process.IsRecommended)} recommendation(s)";
        PreviewSummary = "Preview will show the selected trim and close actions.";
        RefreshStats(updateStatusText: false);
        RaiseProcessSummaryProperties();
        RaiseCommandStates();
    }

    private async Task PreviewAsync()
    {
        Progress = 0.2;
        StatusText = "Building safe preview";
        PreviewProcesses.Clear();

        var preview = await _ramCleanerService.PreviewCleanAsync(_allProcesses);

        foreach (var process in preview)
        {
            PreviewProcesses.Add(process);
        }

        Progress = 1;
        var closeCount = preview.Count(process => process.RecommendedAction == ProcessOptimizationAction.CloseApp);
        var trimCount = preview.Count(process => process.RecommendedAction == ProcessOptimizationAction.TrimWorkingSet || (process.RecommendedAction == ProcessOptimizationAction.CloseApp && !CloseSelectedApps));
        PreviewSummary = $"Preview: {trimCount} trim action(s), {closeCount} graceful close request(s), {MemoryStats.FormatBytes(preview.Sum(process => process.EstimatedReclaimableBytes))} estimated.";
        StatusText = $"Preview ready: {preview.Count} selected candidate(s)";
        RaiseProcessSummaryProperties();
    }

    private async Task CleanAsync()
    {
        Progress = 0.25;
        StatusText = "Optimizing selected memory targets";

        var result = await _ramCleanerService.CleanSelectedAsync(
            _allProcesses,
            new RamCleanOptions
            {
                TrimSelectedApps = TrimSelectedApps,
                CloseSelectedApps = CloseSelectedApps,
                CleanStandbyList = StandbyCleanupEnabled
            });

        MemoryStats = _ramCleanerService.GetMemoryStats();
        AddMemoryHistoryPoint(MemoryStats);

        var cleanedBytes = result.BestMeasuredFreedBytes;
        RamCleanedDisplay = $"{MemoryStats.FormatBytes(cleanedBytes)} cleaned";
        LastCleanDisplay = $"Last clean: {result.CompletedAt:HH:mm:ss}";
        CleanResultMessage = BuildCleanResultMessage(result);
        PreviewSummary = result.Failures.Count == 0
            ? "Clean completed without reported process errors."
            : $"Clean completed with {result.Failures.Count} skipped or denied action(s).";

        Progress = 1;
        StatusText = "Finished RAM optimization";
        ProcessCount = _ramCleanerService.GetProcessCountDisplay();
        await ScanAsync();
        StatusText = "Finished RAM optimization";
    }

    public void RefreshLiveStats()
    {
        RefreshStats(updateStatusText: false);
    }

    private void RefreshStats(bool updateStatusText)
    {
        MemoryStats = _ramCleanerService.GetMemoryStats();
        AddMemoryHistoryPoint(MemoryStats);
        ProcessCount = _ramCleanerService.GetProcessCountDisplay();

        if (updateStatusText)
        {
            StatusText = $"RAM refreshed: {MemoryStats.UsagePercentDisplay} used";
        }
    }

    private static string BuildCleanResultMessage(RamCleanResult result)
    {
        var parts = new List<string>
        {
            $"{result.TrimmedCount} app working set(s) trimmed",
            $"{result.ClosedCount}/{result.CloseRequestedCount} close request(s) completed"
        };

        if (result.StandbyAttempted)
        {
            parts.Add(result.StandbySucceeded ? "standby list purge requested" : result.StandbyMessage);
        }

        if (result.Failures.Count > 0)
        {
            parts.Add($"{result.Failures.Count} action(s) skipped or denied");
        }

        return string.Join("; ", parts) + ".";
    }

    private void SelectRecommended()
    {
        foreach (var process in _allProcesses)
        {
            process.IsSelectedForCleaning = process.IsRecommended;
            _settings.RamCleanerProcessSelections[GetProcessSelectionKey(process)] = process.IsSelectedForCleaning;
        }

        _settingsService.Save(_settings);
        PreviewSummary = $"{SelectedProcessCount} recommended action(s) selected.";
        RaiseProcessSummaryProperties();
    }

    private void DeselectAll()
    {
        foreach (var process in _allProcesses)
        {
            process.IsSelectedForCleaning = false;
            _settings.RamCleanerProcessSelections[GetProcessSelectionKey(process)] = false;
        }

        _settingsService.Save(_settings);
        PreviewSummary = "All process actions deselected. Clean RAM will still run Mist self cleanup.";
        RaiseProcessSummaryProperties();
    }

    private void ApplyProcessFilter()
    {
        var query = SearchText.Trim();
        var filtered = _allProcesses
            .Where(process => MatchesSearch(process, query))
            .OrderByDescending(process => process.IsSelectedForCleaning)
            .ThenByDescending(process => process.IsRecommended)
            .ThenByDescending(process => process.MemoryBytes)
            .ToList();

        Processes.Clear();
        foreach (var process in filtered)
        {
            Processes.Add(process);
        }

        RaiseProcessSummaryProperties();
    }

    private static bool MatchesSearch(ProcessInfo process, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return process.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               process.ProcessId.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
               process.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               process.RecommendedActionDisplay.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void AddMemoryHistoryPoint(MemoryStats stats)
    {
        MemoryHistory.Add(new MemoryGraphPoint
        {
            Height = 10 + Math.Clamp(stats.UsagePercent, 0, 1) * 70
        });

        while (MemoryHistory.Count > 36)
        {
            MemoryHistory.RemoveAt(0);
        }
    }

    private void OnProcessPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ProcessInfo process)
        {
            return;
        }

        if (e.PropertyName == nameof(ProcessInfo.IsProtected))
        {
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

        if (e.PropertyName == nameof(ProcessInfo.IsSelectedForCleaning) ||
            e.PropertyName == nameof(ProcessInfo.IsProtected))
        {
            if (e.PropertyName == nameof(ProcessInfo.IsSelectedForCleaning))
            {
                _settings.RamCleanerProcessSelections[GetProcessSelectionKey(process)] = process.IsSelectedForCleaning;
                _settingsService.Save(_settings);
            }

            RaiseProcessSummaryProperties();
        }
    }

    private static string GetProcessSelectionKey(ProcessInfo process)
    {
        return string.IsNullOrWhiteSpace(process.ExecutableName)
            ? process.Name
            : process.ExecutableName;
    }

    private void ClearProcessSubscriptions()
    {
        foreach (var process in _allProcesses)
        {
            process.PropertyChanged -= OnProcessPropertyChanged;
        }
    }

    private void RaiseProcessSummaryProperties()
    {
        OnPropertyChanged(nameof(SelectedProcessCount));
        OnPropertyChanged(nameof(SelectedItemsDisplay));
        OnPropertyChanged(nameof(SelectedEstimatedBytes));
        OnPropertyChanged(nameof(EstimatedSelectedDisplay));
        OnPropertyChanged(nameof(RecommendedCountDisplay));
        OnPropertyChanged(nameof(VisibleProcessCountDisplay));
        OnPropertyChanged(nameof(HasProcesses));
        OnPropertyChanged(nameof(HasVisibleProcesses));
        OnPropertyChanged(nameof(IsProcessEmptyVisible));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        if (PreviewCommand is AsyncRelayCommand previewCommand)
        {
            previewCommand.RaiseCanExecuteChanged();
        }

        if (SelectRecommendedCommand is RelayCommand selectCommand)
        {
            selectCommand.RaiseCanExecuteChanged();
        }

        if (DeselectAllCommand is RelayCommand deselectCommand)
        {
            deselectCommand.RaiseCanExecuteChanged();
        }
    }
}
