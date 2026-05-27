using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace PCOptimizer.Controls;

public sealed class StarFieldControl : Control
{
    public static readonly StyledProperty<bool> IsPausedProperty =
        AvaloniaProperty.Register<StarFieldControl, bool>(nameof(IsPaused));

    private readonly DispatcherTimer _timer;
    private readonly Star[] _stars;
    private double _phase;
    private bool _isAttached;

    public StarFieldControl()
    {
        IsHitTestVisible = false;
        _stars = CreateStars();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(70)
        };
        _timer.Tick += (_, _) =>
        {
            _phase += 0.004;
            InvalidateVisual();
        };
    }

    public bool IsPaused
    {
        get => GetValue(IsPausedProperty);
        set => SetValue(IsPausedProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        UpdateTimer();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
        _isAttached = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsPausedProperty)
        {
            UpdateTimer();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        foreach (var star in _stars)
        {
            var x = (star.X * bounds.Width + _phase * star.Speed * bounds.Width) % bounds.Width;
            var y = (star.Y * bounds.Height + Math.Sin(_phase * 4 + star.Twinkle) * 5) % bounds.Height;
            var opacity = 0.24 + Math.Sin(_phase * 8 + star.Twinkle) * 0.12 + star.Opacity;
            var brush = new SolidColorBrush(star.Color, Math.Clamp(opacity, 0.16, 0.62));

            context.DrawEllipse(brush, null, new Point(x, y), star.Radius, star.Radius);
        }
    }

    private void UpdateTimer()
    {
        if (IsPaused || !_isAttached)
        {
            _timer.Stop();
        }
        else if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    private static Star[] CreateStars()
    {
        var random = new Random(42);
        var stars = new Star[90];

        for (var index = 0; index < stars.Length; index++)
        {
            stars[index] = new Star(
                random.NextDouble(),
                random.NextDouble(),
                0.6 + random.NextDouble() * 1.8,
                0.015 + random.NextDouble() * 0.04,
                random.NextDouble() * Math.PI * 2,
                0.02 + random.NextDouble() * 0.12,
                index % 5 == 0 ? Color.Parse("#B79CFF") : index % 3 == 0 ? Color.Parse("#8EC5FF") : Colors.White);
        }

        return stars;
    }

    private readonly record struct Star(
        double X,
        double Y,
        double Radius,
        double Speed,
        double Twinkle,
        double Opacity,
        Color Color);
}
