using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using PCOptimizer.Models;
using PCOptimizer.ViewModels;

namespace PCOptimizer.Views;

public partial class OptimizationPanelView : UserControl
{
    public OptimizationPanelView()
    {
        InitializeComponent();
    }

    private void OnOptimizationActionPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Source is Control source &&
            (source.FindAncestorOfType<CheckBox>() is not null ||
             source.FindAncestorOfType<Button>() is not null ||
             source.FindAncestorOfType<Expander>() is not null))
        {
            return;
        }

        if ((e.Source as Control)?.DataContext is not OptimizationAction action ||
            DataContext is not OptimizationPanelViewModel viewModel)
        {
            return;
        }

        viewModel.ToggleActionSelection(action);
        e.Handled = true;
    }
}
