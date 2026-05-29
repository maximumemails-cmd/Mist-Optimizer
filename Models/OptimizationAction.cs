using PCOptimizer.Utilities;

namespace PCOptimizer.Models;

public sealed class OptimizationAction : ViewModelBase
{
    private bool _isSelected;
    private OptimizationStatus _status = OptimizationStatus.Waiting;

    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string ExactAction { get; init; } = "No system command is wired yet. This action only previews or logs.";
    public string UndoAction { get; init; } = "No changes are made, so no undo is required yet.";
    public string Source { get; init; } = "Built-in catalog";
    public string ImplementationStatus { get; init; } = "Not implemented yet";
    public string DisabledReason { get; init; } = string.Empty;
    public string RestartBadgeText { get; init; } = string.Empty;
    public string Reversibility { get; init; } = "Unknown";
    public bool RequiresRestart { get; init; }
    public bool RequiresAdmin { get; init; }
    public bool RequiresSystemWarning { get; init; }
    public bool Reversible { get; init; }
    public RiskLevel RiskLevel { get; init; } = RiskLevel.Safe;
    public bool IsImplemented { get; init; }
    public bool IsEnabled { get; init; } = true;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!IsEnabled)
            {
                value = false;
            }

            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(SelectionMark));
                OnPropertyChanged(nameof(SelectionStateText));
            }
        }
    }

    public OptimizationStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string RestartBadge => string.IsNullOrWhiteSpace(RestartBadgeText) ? (RequiresRestart ? "Restart" : "No restart") : RestartBadgeText;
    public string AdminBadge => RequiresAdmin ? "Admin" : "User";
    public string RiskBadge => RiskLevel.ToString();
    public string ImplementationBadge => ImplementationStatus;
    public string RevertBadge => Reversible ? "Revert available" : $"Revert: {Reversibility}";
    public string WarningBadge => RequiresSystemWarning ? "Warn before apply" : "No write warning";
    public string SelectionMark => IsSelected ? "✓" : string.Empty;
    public string SelectionStateText => IsSelected ? "Selected" : IsEnabled ? "Not selected" : "Unavailable";
    public double RowOpacity => IsEnabled ? 1 : 0.48;

    public void ToggleSelection()
    {
        if (IsEnabled)
        {
            IsSelected = !IsSelected;
        }
    }
}
