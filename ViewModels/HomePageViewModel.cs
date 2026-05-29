using System;
using PCOptimizer.Models;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class HomePageViewModel : ViewModelBase
{
    private HardwareSummary _hardwareSummary = new();
    private MemoryStats _memoryStats = new();
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
    public string HealthSummary => RestartRequiredCount == 0
        ? "System is ready for live optimization."
        : $"{RestartRequiredCount} restart-aware optimization(s) available.";

    public void Refresh(HardwareSummary hardwareSummary, MemoryStats memoryStats, int optimizationCount, int restartRequiredCount)
    {
        HardwareSummary = hardwareSummary;
        MemoryStats = memoryStats;
        OptimizationCount = optimizationCount;
        RestartRequiredCount = restartRequiredCount;
        OnPropertyChanged(nameof(CpuUsageDisplay));
        OnPropertyChanged(nameof(HealthSummary));
    }
}
