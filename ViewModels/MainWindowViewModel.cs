using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private readonly SwuabNetworkService _swuabNetworkService;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _liveStatsTimer;
    private bool _isRestartPromptOpen;
    private bool _areAnimationsPaused;
    private double _globalProgress;
    private HardwareSummary _hardwareSummary;
    private StorageDriveInfo? _primaryStorageDrive;
    private double? _gpuUsagePercent;
    private int _dashboardRefreshTick;
    private AppPageViewModel _currentPage;
    private bool _isDisposed;

    public MainWindowViewModel(
        AppLogger logger,
        SettingsService settingsService,
        OptimizationCatalogService catalogService,
        OptimizationEngine optimizationEngine,
        RamCleanerService ramCleanerService,
        SystemInfoService systemInfoService,
        SwuabNetworkService swuabNetworkService,
        AppSettings settings)
    {
        _logger = logger;
        _settingsService = settingsService;
        _catalogService = catalogService;
        _optimizationEngine = optimizationEngine;
        _ramCleanerService = ramCleanerService;
        _systemInfoService = systemInfoService;
        _swuabNetworkService = swuabNetworkService;
        _settings = settings;

        Brand = new BrandAssets();
        LogPanel = new LogPanelViewModel(logger);
        _hardwareSummary = _systemInfoService.GetHardwareSummary();
        _primaryStorageDrive = _systemInfoService.GetPrimaryStorageDrive();
        _gpuUsagePercent = _systemInfoService.GetGpuUsagePercent();

        Home = new HomePageViewModel();
        RamCleaner = new RamCleanerPanelViewModel(_ramCleanerService, _settings, _settingsService);
        PcSpecs = new PcSpecsPageViewModel(_systemInfoService);
        Swuab = new SwuabPageViewModel(_swuabNetworkService, _logger, _settings, _settingsService);

        var actions = _catalogService.GetAll(_settings.SelectedOptimizationIds).ToList();
        LogCatalogReport(_catalogService.LastReport, actions);

        foreach (var action in actions)
        {
            action.PropertyChanged += OnActionPropertyChanged;
        }

        RestartOptimizations = new OptimizationPanelViewModel(
            "Restart Optimizations",
            "System-level changes that need a restart or deeper review.",
            true,
            actions.Where(action => action.RequiresRestart),
            _optimizationEngine,
            _logger);

        LiveOptimizations = new OptimizationPanelViewModel(
            "Non-Restart Optimizations",
            "Actions that can run or preview during the current session.",
            false,
            actions.Where(action => !action.RequiresRestart),
            _optimizationEngine,
            _logger);

        RamCleaner.ProgressChanged += OnChildProgressChanged;
        RestartOptimizations.ProgressChanged += OnChildProgressChanged;
        LiveOptimizations.ProgressChanged += OnChildProgressChanged;
        RestartOptimizations.RestartRequiredCompleted += (_, _) => IsRestartPromptOpen = true;

        Pages =
        [
            new AppPageViewModel("Home", "System health", Home, SelectPage),
            new AppPageViewModel("RAM Cleaner", "Live memory", RamCleaner, SelectPage),
            new AppPageViewModel("PC Specs", "Hardware profile", PcSpecs, SelectPage),
            new AppPageViewModel("Restart Optimizations", "Restart-aware", RestartOptimizations, SelectPage),
            new AppPageViewModel("Non-Restart Optimizations", "In-session", LiveOptimizations, SelectPage),
            new AppPageViewModel("Swuab", "TCP/IP tuning", Swuab, SelectPage)
        ];

        _currentPage = Pages[0];
        _currentPage.IsCurrent = true;
        RefreshHome();

        RestartNowCommand = new RelayCommand(_ => RestartNowPlaceholder());
        RestartLaterCommand = new RelayCommand(_ => IsRestartPromptOpen = false);
        RefreshHardwareCommand = new RelayCommand(_ => RefreshHardwareSummary());

        _liveStatsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _liveStatsTimer.Tick += (_, _) => RefreshLiveStats();
        _liveStatsTimer.Start();

        _logger.Info("Mist started in dark dashboard mode.");
        _logger.Info($"Settings loaded from {_settingsService.SettingsPath}");
    }

    public BrandAssets Brand { get; }
    public LogPanelViewModel LogPanel { get; }
    public HomePageViewModel Home { get; }
    public RamCleanerPanelViewModel RamCleaner { get; }
    public PcSpecsPageViewModel PcSpecs { get; }
    public SwuabPageViewModel Swuab { get; }
    public OptimizationPanelViewModel RestartOptimizations { get; }
    public OptimizationPanelViewModel LiveOptimizations { get; }
    public ObservableCollection<AppPageViewModel> Pages { get; }
    public ICommand RestartNowCommand { get; }
    public ICommand RestartLaterCommand { get; }
    public ICommand RefreshHardwareCommand { get; }

    public AppPageViewModel CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (_currentPage == value)
            {
                return;
            }

            _currentPage.IsCurrent = false;
            value.IsCurrent = true;

            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(PageIndicatorText));
                OnPropertyChanged(nameof(CurrentPageTitle));
            }
        }
    }

    public string PageIndicatorText => $"{Pages.IndexOf(CurrentPage) + 1} / {Pages.Count}";
    public string CurrentPageTitle => CurrentPage.Title;

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

    public int TotalOptimizationCount => RestartOptimizations.TotalCount + LiveOptimizations.TotalCount;
    public int RestartRequiredCount => RestartOptimizations.TotalCount;

    public void Navigate(int direction)
    {
        if (Pages.Count == 0)
        {
            return;
        }

        var nextIndex = (Pages.IndexOf(CurrentPage) + direction + Pages.Count) % Pages.Count;
        CurrentPage = Pages[nextIndex];
    }

    private void SelectPage(AppPageViewModel page)
    {
        if (Pages.Contains(page))
        {
            CurrentPage = page;
        }
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
        RefreshHome();
    }

    private void LogCatalogReport(OptimizationCatalogReport report, IReadOnlyList<OptimizationAction> actions)
    {
        _logger.Info($"Optimizerstuff path: {report.OptimizerstuffPath}");

        if (report.BatchFileCount == 0)
        {
            _logger.Warning("Optimizerstuff was not found or no .bat files were found. Static audited rows remain visible.");
        }

        if (report.LoadingFailureCount > 0)
        {
            _logger.Warning($"Optimizerstuff loading failures: {report.LoadingFailureCount}.");
        }

        _logger.Info($"Batch files found: {report.BatchFileCount}");
        _logger.Info($"Commands/settings parsed: {report.ParsedCommandCount}");
        _logger.Info($"Classified safe: {report.SafeCount}; caution: {report.CautionCount}; dangerous: {report.DangerousCount}; conflicts: {report.ConflictCount}.");
        _logger.Info($"Restart-required visible rows: {report.VisibleRestartCount}; live visible rows: {report.VisibleNoRestartCount}; restart unknown: {report.UnknownRestartCount}.");
        _logger.Info($"Applyable rows: {report.ApplyableCount}; disabled rows: {report.DisabledCount}; duplicate command/source references consolidated: {report.DuplicateCount}.");
        _logger.Info($"UI restart page rows: {actions.Count(action => action.RequiresRestart)}; UI live page rows: {actions.Count(action => !action.RequiresRestart)}.");
    }

    private void OnChildProgressChanged(object? sender, double progress)
    {
        var values = new List<double>
        {
            RestartOptimizations.Progress,
            LiveOptimizations.Progress,
            RamCleaner.Progress
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
        _primaryStorageDrive = _systemInfoService.GetPrimaryStorageDrive();
        _gpuUsagePercent = _systemInfoService.GetGpuUsagePercent();
        RamCleaner.RefreshStatsCommand.Execute(null);
        PcSpecs.Refresh();
        RefreshHome();
        _logger.Info("Hardware summary, specs, and RAM statistics refreshed.");
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

        _dashboardRefreshTick++;
        _gpuUsagePercent = _systemInfoService.GetGpuUsagePercent();
        if (_dashboardRefreshTick % 5 == 0)
        {
            _primaryStorageDrive = _systemInfoService.GetPrimaryStorageDrive();
        }

        RamCleaner.RefreshLiveStats();
        RefreshHome();
    }

    private void RefreshHome()
    {
        Home.Refresh(
            HardwareSummary,
            RamCleaner.MemoryStats,
            TotalOptimizationCount,
            RestartRequiredCount,
            _systemInfoService.IsAdministrator(),
            _gpuUsagePercent,
            _primaryStorageDrive);
        OnPropertyChanged(nameof(TotalOptimizationCount));
        OnPropertyChanged(nameof(RestartRequiredCount));
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
