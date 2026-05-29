using PCOptimizer.Utilities;

namespace PCOptimizer.Models;

public sealed class ProcessInfo : ViewModelBase
{
    private bool _isProtected;
    private bool _isSelectedForCleaning;
    private string _reason = string.Empty;

    public string Name { get; init; } = string.Empty;
    public int ProcessId { get; init; }
    public long MemoryBytes { get; init; }
    public long PrivateMemoryBytes { get; init; }
    public long EstimatedReclaimableBytes { get; init; }
    public bool IsSystemProcess { get; init; }
    public bool IsMistProcess { get; init; }
    public bool HasVisibleWindow { get; init; }
    public bool NeverAutoSelect { get; init; }
    public string WindowTitle { get; init; } = string.Empty;
    public string ExecutableName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = "Background";
    public ProcessOptimizationAction RecommendedAction { get; init; }
    public bool IsRecommended => RecommendedAction != ProcessOptimizationAction.Ignore && CanSelect && !NeverAutoSelect;
    public bool CanSelect => !IsProtected && !IsSystemProcess && RecommendedAction != ProcessOptimizationAction.Ignore;
    public string Reason
    {
        get => _reason;
        set => SetProperty(ref _reason, value);
    }

    public bool IsProtected
    {
        get => _isProtected;
        set
        {
            if (SetProperty(ref _isProtected, value) && value)
            {
                IsSelectedForCleaning = false;
            }

            OnPropertyChanged(nameof(CanSelect));
            OnPropertyChanged(nameof(IsRecommended));
            OnPropertyChanged(nameof(ProtectionDisplay));
        }
    }

    public bool IsSelectedForCleaning
    {
        get => _isSelectedForCleaning;
        set
        {
            if (!CanSelect)
            {
                value = false;
            }

            SetProperty(ref _isSelectedForCleaning, value);
        }
    }

    public string MemoryDisplay => MemoryBytes <= 0 ? "Unknown" : $"{MemoryBytes / 1024d / 1024d:N0} MB";
    public string EstimatedReclaimableDisplay => EstimatedReclaimableBytes <= 0 ? "Review" : $"~{MemoryStats.FormatBytes(EstimatedReclaimableBytes)}";
    public string ProcessIdDisplay => $"PID {ProcessId}";
    public string RecommendedActionDisplay => RecommendedAction switch
    {
        ProcessOptimizationAction.CloseApp => "Close",
        ProcessOptimizationAction.TrimWorkingSet => "Trim",
        _ => "Ignore"
    };

    public string ProtectionDisplay => IsProtected || IsSystemProcess
        ? "Protected"
        : Category;
}
