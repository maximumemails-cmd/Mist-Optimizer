using System;
using Avalonia;
using Avalonia.Styling;
using PCOptimizer.Models;

namespace PCOptimizer.Services;

public sealed class ThemeService
{
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;

    public ThemeService(SettingsService settingsService, AppSettings settings)
    {
        _settingsService = settingsService;
        _settings = settings;
        ApplyTheme(_settings.Theme);
    }

    public event EventHandler? ThemeChanged;

    public string CurrentTheme { get; private set; } = "Dark";
    public bool IsDark => string.Equals(CurrentTheme, "Dark", StringComparison.OrdinalIgnoreCase);

    public void ToggleTheme()
    {
        ApplyTheme(IsDark ? "Light" : "Dark");
    }

    public void ApplyTheme(string theme)
    {
        CurrentTheme = string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";
        _settings.Theme = CurrentTheme;
        _settingsService.Save(_settings);

        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
