using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace PCOptimizer.Controls;

public sealed class DonutMeterControl : Control
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<DonutMeterControl, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<double> PercentageProperty =
        AvaloniaProperty.Register<DonutMeterControl, double>(nameof(Percentage));

    public static readonly StyledProperty<string> CenterTextProperty =
        AvaloniaProperty.Register<DonutMeterControl, string>(nameof(CenterText), "--");

    public static readonly StyledProperty<string> SubtextProperty =
        AvaloniaProperty.Register<DonutMeterControl, string>(nameof(Subtext), string.Empty);

    public static readonly StyledProperty<string> StatusTextProperty =
        AvaloniaProperty.Register<DonutMeterControl, string>(nameof(StatusText), string.Empty);

    public static readonly StyledProperty<Color> AccentColorProperty =
        AvaloniaProperty.Register<DonutMeterControl, Color>(nameof(AccentColor), Color.Parse("#75C7FF"));

    private readonly DispatcherTimer _animationTimer;
    private double _displayPercentage;

    public DonutMeterControl()
    {
        MinHeight = 214;
        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _animationTimer.Tick += (_, _) => AnimateFill();
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public double Percentage
    {
        get => GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }

    public string CenterText
    {
        get => GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    public string Subtext
    {
        get => GetValue(SubtextProperty);
        set => SetValue(SubtextProperty, value);
    }

    public string StatusText
    {
        get => GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public Color AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _displayPercentage = Math.Clamp(Percentage, 0, 1);
        _animationTimer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _animationTimer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PercentageProperty ||
            change.Property == TitleProperty ||
            change.Property == CenterTextProperty ||
            change.Property == SubtextProperty ||
            change.Property == StatusTextProperty ||
            change.Property == AccentColorProperty)
        {
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        var titleBrush = Brush("#F7FAFF");
        var mutedBrush = Brush("#9EAAC1");
        var emptyBrush = Brush("#314162", 0.48);
        var accentBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(AccentColor, 0),
                new GradientStop(Color.Parse("#B77DFF"), 1)
            }
        };

        DrawText(context, Title, 16, FontWeight.SemiBold, titleBrush, new Rect(0, 0, bounds.Width, 30), TextAlignment.Center);

        var footerHeight = string.IsNullOrWhiteSpace(StatusText) ? 34 : 56;
        var ringArea = new Rect(0, 34, bounds.Width, Math.Max(84, bounds.Height - 34 - footerHeight));
        var ringSize = Math.Min(bounds.Width * 0.68, ringArea.Height);
        ringSize = Math.Clamp(ringSize, 92, 148);
        var ringTop = ringArea.Y + Math.Max(0, (ringArea.Height - ringSize) / 2);
        var ringRect = new Rect((bounds.Width - ringSize) / 2, ringTop, ringSize, ringSize);
        var thickness = Math.Clamp(ringSize * 0.11, 11, 18);
        var radius = (ringSize - thickness) / 2;
        var center = ringRect.Center;

        context.DrawEllipse(null, new Pen(emptyBrush, thickness), center, radius, radius);
        DrawArc(context, center, radius, thickness, Math.Clamp(_displayPercentage, 0, 1), accentBrush);

        var centerFontSize = ringSize > 120 ? 30 : 24;
        DrawText(context, CenterText, centerFontSize, FontWeight.SemiBold, titleBrush, ringRect, TextAlignment.Center);

        var subtextRect = new Rect(12, bounds.Height - footerHeight + 6, Math.Max(1, bounds.Width - 24), 24);
        DrawText(context, Subtext, 12, FontWeight.Normal, mutedBrush, subtextRect, TextAlignment.Center);

        if (!string.IsNullOrWhiteSpace(StatusText))
        {
            var statusRect = new Rect(12, subtextRect.Bottom + 2, Math.Max(1, bounds.Width - 24), 20);
            DrawText(context, StatusText, 11, FontWeight.SemiBold, mutedBrush, statusRect, TextAlignment.Center);
        }
    }

    private void AnimateFill()
    {
        var target = Math.Clamp(Percentage, 0, 1);
        var delta = target - _displayPercentage;

        if (Math.Abs(delta) < 0.002)
        {
            _displayPercentage = target;
        }
        else
        {
            _displayPercentage += delta * 0.18;
        }

        InvalidateVisual();
    }

    private static void DrawArc(DrawingContext context, Point center, double radius, double thickness, double percentage, IBrush brush)
    {
        if (percentage <= 0)
        {
            return;
        }

        var pen = new Pen(brush, thickness, lineCap: PenLineCap.Round);

        if (percentage >= 0.995)
        {
            context.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        var startAngle = -90d;
        var endAngle = startAngle + 360d * percentage;
        var start = PointOnCircle(center, radius, startAngle);
        var end = PointOnCircle(center, radius, endAngle);

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(start, isFilled: false);
            stream.ArcTo(
                end,
                new Size(radius, radius),
                rotationAngle: 0,
                isLargeArc: percentage > 0.5,
                SweepDirection.Clockwise);
            stream.EndFigure(isClosed: false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        var angle = angleDegrees * Math.PI / 180d;
        return new Point(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius);
    }

    private static void DrawText(
        DrawingContext context,
        string text,
        double fontSize,
        FontWeight weight,
        IBrush brush,
        Rect rect,
        TextAlignment alignment)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, weight),
            fontSize,
            brush)
        {
            TextAlignment = alignment,
            MaxTextWidth = rect.Width,
            MaxTextHeight = rect.Height,
            Trimming = TextTrimming.CharacterEllipsis
        };

        var x = rect.X;
        var y = rect.Y + Math.Max(0, (rect.Height - formattedText.Height) / 2);
        context.DrawText(formattedText, new Point(x, y));
    }

    private static SolidColorBrush Brush(string color, double opacity = 1) =>
        new(Color.Parse(color), opacity);
}
