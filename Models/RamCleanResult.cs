using System;
using System.Collections.Generic;
using System.Linq;

namespace PCOptimizer.Models;

public sealed class RamCleanResult
{
    public long ObservedSystemFreedBytes { get; init; }
    public long MeasuredProcessReclaimedBytes { get; init; }
    public long MistReclaimedBytes { get; init; }
    public int SelectedCount { get; init; }
    public int TrimmedCount { get; init; }
    public int CloseRequestedCount { get; init; }
    public int ClosedCount { get; init; }
    public bool StandbyAttempted { get; init; }
    public bool StandbySucceeded { get; init; }
    public string StandbyMessage { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; } = DateTime.Now;
    public IReadOnlyList<string> Messages { get; init; } = [];
    public IReadOnlyList<string> Failures { get; init; } = [];

    public long BestMeasuredFreedBytes => Math.Max(ObservedSystemFreedBytes, MeasuredProcessReclaimedBytes + MistReclaimedBytes);
    public string Summary => string.Join(" ", Messages.Take(3));
}
