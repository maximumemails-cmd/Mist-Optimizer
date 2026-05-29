namespace PCOptimizer.Models;

public sealed class OptimizationCatalogReport
{
    public string OptimizerstuffPath { get; set; } = "Not found";
    public int BatchFileCount { get; set; }
    public int ParsedCommandCount { get; set; }
    public int LoadingFailureCount { get; set; }
    public int SafeCount { get; set; }
    public int CautionCount { get; set; }
    public int DangerousCount { get; set; }
    public int ConflictCount { get; set; }
    public int DuplicateCount { get; set; }
    public int RequiresRestartCount { get; set; }
    public int NoRestartCount { get; set; }
    public int UnknownRestartCount { get; set; }
    public int VisibleRestartCount { get; set; }
    public int VisibleNoRestartCount { get; set; }
    public int ApplyableCount { get; set; }
    public int DisabledCount { get; set; }
}
