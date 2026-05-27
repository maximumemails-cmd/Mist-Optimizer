using Avalonia.Controls;

namespace PCOptimizer.Views;

public partial class DashboardView : UserControl
{
    private bool _isStacked;

    public DashboardView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateLayoutMode();
        AttachedToVisualTree += (_, _) => UpdateLayoutMode();
    }

    private void UpdateLayoutMode()
    {
        var shouldStack = Bounds.Width < 900;

        if (shouldStack == _isStacked)
        {
            return;
        }

        _isStacked = shouldStack;

        LayoutRoot.ColumnDefinitions.Clear();
        LayoutRoot.RowDefinitions.Clear();

        if (shouldStack)
        {
            LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            LayoutRoot.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            LayoutRoot.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            LayoutRoot.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            Grid.SetRow(RamPanel, 0);
            Grid.SetColumn(RamPanel, 0);
            Grid.SetColumnSpan(RamPanel, 1);
            Grid.SetRow(RestartPanel, 1);
            Grid.SetColumn(RestartPanel, 0);
            Grid.SetRow(NoRestartPanel, 2);
            Grid.SetColumn(NoRestartPanel, 0);
            return;
        }

        LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        LayoutRoot.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        LayoutRoot.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        Grid.SetRow(RamPanel, 0);
        Grid.SetColumn(RamPanel, 0);
        Grid.SetColumnSpan(RamPanel, 2);
        Grid.SetRow(RestartPanel, 1);
        Grid.SetColumn(RestartPanel, 0);
        Grid.SetRow(NoRestartPanel, 1);
        Grid.SetColumn(NoRestartPanel, 1);
    }
}
