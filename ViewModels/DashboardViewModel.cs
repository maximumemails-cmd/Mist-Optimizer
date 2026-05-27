namespace PCOptimizer.ViewModels;

public sealed class DashboardViewModel
{
    public DashboardViewModel(
        OptimizationPanelViewModel restartPanel,
        OptimizationPanelViewModel noRestartPanel,
        RamCleanerPanelViewModel ramCleanerPanel)
    {
        RestartPanel = restartPanel;
        NoRestartPanel = noRestartPanel;
        RamCleanerPanel = ramCleanerPanel;
    }

    public OptimizationPanelViewModel RestartPanel { get; }
    public OptimizationPanelViewModel NoRestartPanel { get; }
    public RamCleanerPanelViewModel RamCleanerPanel { get; }
}
