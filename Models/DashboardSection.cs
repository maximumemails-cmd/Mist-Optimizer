using System.Collections.ObjectModel;

namespace PCOptimizer.Models;

public sealed class DashboardSection
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; set; } = "Ready";
    public ObservableCollection<OptimizationAction> Actions { get; } = new();
}
