namespace PCOptimizer.Models;

public sealed class DashboardMetric
{
    public string Title { get; init; } = string.Empty;
    public double Percentage { get; init; }
    public string CenterText { get; init; } = "--";
    public string Subtext { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
}
