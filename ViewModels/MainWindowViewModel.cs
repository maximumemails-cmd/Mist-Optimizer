using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Threading;
using PCOptimizer.Models;
using PCOptimizer.Services;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly AppLogger _logger;
    private readonly SettingsService _settingsService;
    private readonly OptimizationCatalogService _catalogService;
    private readonly OptimizationEngine _optimizationEngine;
    private readonly RamCleanerService _ramCleanerService;
    private readonly SystemInfoService _systemInfoService;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _liveStatsTimer;
    private DashboardViewModel _dashboard;
    private bool _isRestartPromptOpen;
    private bool _areAnimationsPaused;
    private double _globalProgress;
    private bool _showAdvancedTweaks;
    private HardwareSummary _hardwareSummary;
    private bool _isDisposed;

    public MainWindowViewModel(
        AppLogger logger,
        SettingsService settingsService,
        ThemeService themeService,
        OptimizationCatalogService catalogService,
        OptimizationEngine optimizationEngine,
        RamCleanerService ramCleanerService,
        SystemInfoService systemInfoService,
        AppSettings settings)
    {
        _logger = logger;
        _settingsService = settingsService;
        _catalogService = catalogService;
        _optimizationEngine = optimizationEngine;
        _ramCleanerService = ramCleanerService;
        _systemInfoService = systemInfoService;
        _settings = settings;
        _showAdvancedTweaks = _settings.ShowAdvancedTweaks;

        ThemeToggle = new ThemeToggleViewModel(themeService);
        LogPanel = new LogPanelViewModel(logger);
        _hardwareSummary = _systemInfoService.GetHardwareSummary();
        _dashboard = BuildDashboard();

        RestartNowCommand = new RelayCommand(_ => RestartNowPlaceholder());
        RestartLaterCommand = new RelayCommand(_ => IsRestartPromptOpen = false);
        RefreshHardwareCommand = new RelayCommand(_ => RefreshHardwareSummary());
        _liveStatsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _liveStatsTimer.Tick += (_, _) => RefreshLiveStats();
        _liveStatsTimer.Start();

        _logger.Info("PC Optimizer started in safe optimisation framework mode.");
        _logger.Info($"Settings loaded from {_settingsService.SettingsPath}");
    }

    public ThemeToggleViewModel ThemeToggle { get; }
    public LogPanelViewModel LogPanel { get; }
    public ICommand RestartNowCommand { get; }
    public ICommand RestartLaterCommand { get; }
    public ICommand RefreshHardwareCommand { get; }

    public DashboardViewModel Dashboard
    {
        get => _dashboard;
        private set => SetProperty(ref _dashboard, value);
    }

    public bool IsRestartPromptOpen
    {
        get => _isRestartPromptOpen;
        set => SetProperty(ref _isRestartPromptOpen, value);
    }

    public bool AreAnimationsPaused
    {
        get => _areAnimationsPaused;
        set
        {
            if (SetProperty(ref _areAnimationsPaused, value))
            {
                UpdateLivePollingState();
            }
        }
    }

    public HardwareSummary HardwareSummary
    {
        get => _hardwareSummary;
        private set => SetProperty(ref _hardwareSummary, value);
    }

    public double GlobalProgress
    {
        get => _globalProgress;
        set => SetProperty(ref _globalProgress, value);
    }

    public bool ShowAdvancedTweaks
    {
        get => _showAdvancedTweaks;
        set
        {
            if (!SetProperty(ref _showAdvancedTweaks, value))
            {
                return;
            }

            _settings.ShowAdvancedTweaks = value;
            _settingsService.Save(_settings);
            Dashboard = BuildDashboard();
            _logger.Info(value ? "Advanced tweaks are visible." : "Advanced tweaks are hidden.");
        }
    }

    private DashboardViewModel BuildDashboard()
    {
        var actions = _catalogService.GetAll(_settings.SelectedOptimizationIds).ToList();
        LogCatalogReport(_catalogService.LastReport, actions);

        foreach (var action in actions)
        {
            action.PropertyChanged += OnActionPropertyChanged;
        }

        var restartPanel = new OptimizationPanelViewModel(
            "Restart Required Optimisations",
            "Changes that would need a restart when safely implemented.",
            true,
            actions.Where(action => action.RequiresRestart),
            _optimizationEngine,
            _logger);

        var noRestartPanel = new OptimizationPanelViewModel(
            "No-Restart Optimisations",
            "Implemented safe actions that can finish in-session.",
            false,
            actions.Where(action => !action.RequiresRestart),
            _optimizationEngine,
            _logger);

        var ramPanel = new RamCleanerPanelViewModel(_ramCleanerService, _settings, _settingsService);

        restartPanel.ProgressChanged += OnChildProgressChanged;
        noRestartPanel.ProgressChanged += OnChildProgressChanged;
        ramPanel.ProgressChanged += OnChildProgressChanged;
        restartPanel.RestartRequiredCompleted += (_, _) => IsRestartPromptOpen = true;

        return new DashboardViewModel(restartPanel, noRestartPanel, ramPanel);
    }

    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not OptimizationAction action || e.PropertyName != nameof(OptimizationAction.IsSelected))
        {
            return;
        }

        if (action.IsSelected && !_settings.SelectedOptimizationIds.Contains(action.Id))
        {
            _settings.SelectedOptimizationIds.Add(action.Id);
        }
        else if (!action.IsSelected)
        {
            _settings.SelectedOptimizationIds.Remove(action.Id);
        }

        _settingsService.Save(_settings);
    }

    private void LogCatalogReport(OptimizationCatalogReport report, IReadOnlyList<OptimizationAction> actions)
    {
        _logger.Info($"Optimizerstuff path: {report.OptimizerstuffPath}");

        if (report.BatchFileCount == 0)
        {
            _logger.Warning("Optimizerstuff was not found or no .bat files were found. Batch-derived catalogue rows are still shown from the static audit, but live scan counts are unavailable.");
        }

        _logger.Info($"Batch files found: {report.BatchFileCount}");
        _logger.Info($"Commands/settings parsed: {report.ParsedCommandCount}");
        _logger.Info($"Classified safe: {report.SafeCount}; caution: {report.CautionCount}; dangerous: {report.DangerousCount}; conflicts: {report.ConflictCount}.");
        _logger.Info($"Restart-required visible rows: {report.VisibleRestartCount}; no-restart visible rows: {report.VisibleNoRestartCount}; restart unknown: {report.UnknownRestartCount}.");
        _logger.Info($"Applyable rows: {report.ApplyableCount}; disabled rows: {report.DisabledCount}; duplicate command/source references consolidated: {report.DuplicateCount}.");
        _logger.Info($"UI restart panel rows: {actions.Count(action => action.RequiresRestart)}; UI no-restart panel rows: {actions.Count(action => !action.RequiresRestart)}.");
    }

    private void OnChildProgressChanged(object? sender, double progress)
    {
        var values = new List<double>
        {
            Dashboard.RestartPanel.Progress,
            Dashboard.NoRestartPanel.Progress,
            Dashboard.RamCleanerPanel.Progress
        };

        GlobalProgress = values.Average();
    }

    private void RestartNowPlaceholder()
    {
        if (!_systemInfoService.IsWindows)
        {
            _logger.Warning("Restart action only available on Windows.");
        }
        else
        {
            _logger.Warning("Restart Now is placeholder-only. No restart command was sent.");
        }

        IsRestartPromptOpen = false;
    }

    private void RefreshHardwareSummary()
    {
        HardwareSummary = _systemInfoService.GetHardwareSummary();
        Dashboard.RamCleanerPanel.RefreshStatsCommand.Execute(null);
        _logger.Info("Hardware summary and process count refreshed.");
    }

    private void RefreshLiveStats()
    {
        if (_isDisposed || AreAnimationsPaused)
        {
            return;
        }

        HardwareSummary = new HardwareSummary
        {
            Cpu = HardwareSummary.Cpu,
            CpuUsage = _systemInfoService.GetCpuUsageDisplay(),
            Gpu = HardwareSummary.Gpu,
            Ram = HardwareSummary.Ram,
            Storage = HardwareSummary.Storage,
            Processes = _systemInfoService.GetProcessCountDisplay()
        };

        Dashboard.RamCleanerPanel.RefreshLiveStats();
    }

    private void UpdateLivePollingState()
    {
        if (_isDisposed)
        {
            return;
        }

        if (AreAnimationsPaused)
        {
            _liveStatsTimer.Stop();
        }
        else if (!_liveStatsTimer.IsEnabled)
        {
            _liveStatsTimer.Start();
            RefreshLiveStats();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _liveStatsTimer.Stop();
    }
}
