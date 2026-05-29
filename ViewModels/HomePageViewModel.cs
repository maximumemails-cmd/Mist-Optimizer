using System;
using System.Globalization;
using PCOptimizer.Models;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class HomePageViewModel : ViewModelBase
{
    private HardwareSummary _hardwareSummary = new();
    private MemoryStats _memoryStats = new();
    private DashboardMetric _ramMetric = new();
    private DashboardMetric _cpuMetric = new();
    private DashboardMetric _gpuMetric = new();
    private DashboardMetric _storageMetric = new();
    private bool _isAdministrator;
    private int _optimizationCount;
    private int _restartRequiredCount;

    public BrandAssets Brand { get; } = new();

    public HardwareSummary HardwareSummary
    {
        get => _hardwareSummary;
        set => SetProperty(ref _hardwareSummary, value);
    }

    public MemoryStats MemoryStats
    {
        get => _memoryStats;
        set => SetProperty(ref _memoryStats, value);
    }

    public DashboardMetric RamMetric
    {
        get => _ramMetric;
        private set => SetProperty(ref _ramMetric, value);
    }

    public DashboardMetric CpuMetric
    {
        get => _cpuMetric;
        private set => SetProperty(ref _cpuMetric, value);
    }

    public DashboardMetric GpuMetric
    {
        get => _gpuMetric;
        private set => SetProperty(ref _gpuMetric, value);
    }

    public DashboardMetric StorageMetric
    {
        get => _storageMetric;
        private set => SetProperty(ref _storageMetric, value);
    }

    public bool IsAdministrator
    {
        get => _isAdministrator;
        set
        {
            if (SetProperty(ref _isAdministrator, value))
            {
                OnPropertyChanged(nameof(AdminStatusText));
                OnPropertyChanged(nameof(AdminSummaryText));
                OnPropertyChanged(nameof(IsAdminWarningVisible));
            }
        }
    }

    public int OptimizationCount
    {
        get => _optimizationCount;
        set
        {
            if (SetProperty(ref _optimizationCount, value))
            {
                OnPropertyChanged(nameof(HealthSummary));
            }
        }
    }

    public int RestartRequiredCount
    {
        get => _restartRequiredCount;
        set
        {
            if (SetProperty(ref _restartRequiredCount, value))
            {
                OnPropertyChanged(nameof(HealthSummary));
            }
        }
    }

    public string CpuUsageDisplay => HardwareSummary.CpuUsage.Replace("CPU: ", string.Empty, StringComparison.OrdinalIgnoreCase);

    public string AdminStatusText => IsAdministrator ? "Admin: Active" : "Admin: Not active";

    public string AdminSummaryText => IsAdministrator
        ? "Administrator permissions are available for system optimizations."
        : "Mist is not running as administrator. Some optimizations may not work.";

    public bool IsAdminWarningVisible => !IsAdministrator;

    public string HealthSummary => RestartRequiredCount == 0
        ? "System is ready for live optimization."
        : $"{RestartRequiredCount} restart-aware optimization(s) available.";

    public void Refresh(
        HardwareSummary hardwareSummary,
        MemoryStats memoryStats,
        int optimizationCount,
        int restartRequiredCount,
        bool isAdministrator,
        double? gpuUsagePercent,
        StorageDriveInfo? primaryStorage)
    {
        HardwareSummary = hardwareSummary;
        MemoryStats = memoryStats;
        OptimizationCount = optimizationCount;
        RestartRequiredCount = restartRequiredCount;
        IsAdministrator = isAdministrator;

        RamMetric = BuildRamMetric(memoryStats);
        CpuMetric = BuildCpuMetric(hardwareSummary);
        GpuMetric = BuildGpuMetric(hardwareSummary, gpuUsagePercent);
        StorageMetric = BuildStorageMetric(primaryStorage);

        OnPropertyChanged(nameof(CpuUsageDisplay));
        OnPropertyChanged(nameof(HealthSummary));
    }

    private static DashboardMetric BuildRamMetric(MemoryStats memoryStats)
    {
        var known = memoryStats.TotalBytes > 0;
        return new DashboardMetric
        {
            Title = "RAM Usage",
            Percentage = known ? memoryStats.UsagePercent : 0,
            CenterText = known ? memoryStats.UsagePercentDisplay : "--",
            Subtext = known ? $"{memoryStats.UsedDisplay} / {memoryStats.TotalDisplay} used" : "RAM data unavailable",
            StatusText = known ? $"Updated {memoryStats.TimestampDisplay}" : "Unavailable"
        };
    }

    private static DashboardMetric BuildCpuMetric(HardwareSummary hardwareSummary)
    {
        var usage = ParsePercent(hardwareSummary.CpuUsage);
        return new DashboardMetric
        {
            Title = "CPU Usage",
            Percentage = usage ?? 0,
            CenterText = usage.HasValue ? $"{usage.Value:P0}" : "--",
            Subtext = hardwareSummary.Processes,
            StatusText = usage.HasValue ? "Live CPU sample" : "Measuring CPU"
        };
    }

    private static DashboardMetric BuildGpuMetric(HardwareSummary hardwareSummary, double? gpuUsagePercent)
    {
        var gpuName = string.IsNullOrWhiteSpace(hardwareSummary.Gpu) || hardwareSummary.Gpu == "Unknown"
            ? "GPU data unavailable"
            : hardwareSummary.Gpu;

        return new DashboardMetric
        {
            Title = "GPU Usage",
            Percentage = gpuUsagePercent ?? 0,
            CenterText = gpuUsagePercent.HasValue ? $"{gpuUsagePercent.Value:P0}" : "--",
            Subtext = gpuName,
            StatusText = gpuUsagePercent.HasValue ? "Live GPU sample" : "GPU data unavailable"
        };
    }

    private static DashboardMetric BuildStorageMetric(StorageDriveInfo? primaryStorage)
    {
        if (primaryStorage is null || primaryStorage.TotalBytes <= 0)
        {
            return new DashboardMetric
            {
                Title = "Storage Usage",
                Percentage = 0,
                CenterText = "--",
                Subtext = "Storage data unavailable",
                StatusText = "Unavailable"
            };
        }

        return new DashboardMetric
        {
            Title = "Storage Usage",
            Percentage = primaryStorage.UsagePercent,
            CenterText = primaryStorage.UsagePercentDisplay,
            Subtext = $"{primaryStorage.UsedDisplay} / {primaryStorage.TotalDisplay} used",
            StatusText = primaryStorage.Title
        };
    }

    private static double? ParsePercent(string text)
    {
        var percentIndex = text.IndexOf('%', StringComparison.Ordinal);
        if (percentIndex <= 0)
        {
            return null;
        }

        var start = percentIndex - 1;
        while (start > 0 && (char.IsDigit(text[start - 1]) || text[start - 1] == '.' || text[start - 1] == ','))
        {
            start--;
        }

        var numberText = text[start..percentIndex];
        return double.TryParse(numberText, NumberStyles.Float, CultureInfo.CurrentCulture, out var number) ||
               double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            ? Math.Clamp(number / 100d, 0, 1)
            : null;
    }
}
