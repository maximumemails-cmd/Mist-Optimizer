using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using PCOptimizer.ViewModels;

namespace PCOptimizer.Controls;

public partial class ThemeToggleControl : UserControl
{
    private readonly DispatcherTimer _timer;
    private double _startX;
    private double _targetX;
    private DateTime _startedAt;
    private const double LightX = 0;
    private const double DarkX = 40;
    private const double DurationMs = 220;

    public ThemeToggleControl()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnAnimationTick;
        DataContextChanged += (_, _) => AttachViewModel();
    }

    private void AttachViewModel()
    {
        if (DataContext is ThemeToggleViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            ThumbX = viewModel.IsDarkTheme ? DarkX : LightX;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ThemeToggleViewModel viewModel && e.PropertyName == nameof(ThemeToggleViewModel.IsDarkTheme))
        {
            AnimateThumb(viewModel.IsDarkTheme ? DarkX : LightX);
        }
    }

    private void AnimateThumb(double targetX)
    {
        _startX = ThumbX;
        _targetX = targetX;
        _startedAt = DateTime.UtcNow;
        _timer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        var elapsed = (DateTime.UtcNow - _startedAt).TotalMilliseconds;
        var amount = Math.Clamp(elapsed / DurationMs, 0, 1);
        var eased = 1 - Math.Pow(1 - amount, 3);
        ThumbX = _startX + (_targetX - _startX) * eased;

        if (amount >= 1)
        {
            ThumbX = _targetX;
            _timer.Stop();
        }
    }

    private double ThumbX
    {
        get => Thumb.RenderTransform is TranslateTransform transform ? transform.X : 0;
        set
        {
            if (Thumb.RenderTransform is TranslateTransform transform)
            {
                transform.X = value;
            }
        }
    }
}
