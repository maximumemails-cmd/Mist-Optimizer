namespace PCOptimizer.Models;

public sealed class RamCleanOptions
{
    public bool TrimSelectedApps { get; init; }
    public bool CloseSelectedApps { get; init; }
    public bool CleanStandbyList { get; init; }
}
