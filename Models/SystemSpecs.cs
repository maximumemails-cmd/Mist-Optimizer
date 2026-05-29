using System.Collections.Generic;

namespace PCOptimizer.Models;

public sealed class SystemSpecs
{
    public string Cpu { get; init; } = "Unknown";
    public string Gpu { get; init; } = "Unknown";
    public string Ram { get; init; } = "Unknown";
    public string Motherboard { get; init; } = "Unknown";
    public string WindowsVersion { get; init; } = "Unknown";
    public IReadOnlyList<StorageDriveInfo> Drives { get; init; } = [];
}
