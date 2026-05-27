using System;

namespace PCOptimizer.Models;

public sealed class OptimizerStateEntry
{
    public string OptimizationId { get; set; } = string.Empty;
    public string SourceBatchFile { get; set; } = string.Empty;
    public string OriginalValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime DateApplied { get; set; }
    public bool RestartNeeded { get; set; }
    public bool RevertAvailable { get; set; }
}
