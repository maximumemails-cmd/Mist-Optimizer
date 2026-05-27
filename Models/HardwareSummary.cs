namespace PCOptimizer.Models;

public sealed class HardwareSummary
{
    public string Cpu { get; init; } = "Unknown";
    public string CpuUsage { get; init; } = "CPU: Unknown";
    public string Gpu { get; init; } = "Unknown";
    public string Ram { get; init; } = "Unknown";
    public string Storage { get; init; } = "Unknown";
    public string Processes { get; init; } = "Processes: Unknown";
}
