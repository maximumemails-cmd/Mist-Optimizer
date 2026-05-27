namespace PCOptimizer.Models;

public enum OptimizationStatus
{
    Waiting,
    Running,
    Completed,
    Failed,
    Skipped,
    NeedsRestart,
    NotImplemented
}
