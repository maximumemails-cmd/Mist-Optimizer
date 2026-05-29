using Avalonia.Controls;
using PCOptimizer.ViewModels;
using System;

namespace PCOptimizer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => UpdateAnimationState();
        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty || e.Property == IsVisibleProperty)
            {
                UpdateAnimationState();
            }
        };
        Activated += (_, _) => UpdateAnimationState();
        Deactivated += (_, _) => UpdateAnimationState(paused: true);
        Closed += (_, _) =>
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
    }

    private void UpdateAnimationState(bool? paused = null)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.AreAnimationsPaused = paused ?? WindowState == WindowState.Minimized || !IsVisible;
    }

}
