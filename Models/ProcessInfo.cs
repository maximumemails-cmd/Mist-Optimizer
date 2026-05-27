using PCOptimizer.Utilities;

namespace PCOptimizer.Models;

public sealed class ProcessInfo : ViewModelBase
{
    private bool _isProtected;
    private bool _isSelectedForCleaning;

    public string Name { get; init; } = string.Empty;
    public int ProcessId { get; init; }
    public long MemoryBytes { get; init; }
    public bool IsSystemProcess { get; init; }
    public string Reason { get; set; } = string.Empty;

    public bool IsProtected
    {
        get => _isProtected;
        set
        {
            if (SetProperty(ref _isProtected, value) && value)
            {
                IsSelectedForCleaning = false;
            }
        }
    }

    public bool IsSelectedForCleaning
    {
        get => _isSelectedForCleaning;
        set
        {
            if (IsProtected || IsSystemProcess)
            {
                value = false;
            }

            SetProperty(ref _isSelectedForCleaning, value);
        }
    }

    public string MemoryDisplay => MemoryBytes <= 0 ? "Unknown" : $"{MemoryBytes / 1024d / 1024d:N0} MB";
}
