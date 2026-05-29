using Avalonia.Controls;
using Avalonia.Input;
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
        KeyDown += OnKeyDown;
    }

    private void UpdateAnimationState(bool? paused = null)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.AreAnimationsPaused = paused ?? WindowState == WindowState.Minimized || !IsVisible;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Right)
        {
            viewModel.Navigate(1);
            e.Handled = true;
        }
        else if (e.Key == Key.Left)
        {
            viewModel.Navigate(-1);
            e.Handled = true;
        }
    }
}
