using System.Windows.Input;
using PCOptimizer.Services;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class ThemeToggleViewModel : ViewModelBase
{
    private readonly ThemeService _themeService;

    public ThemeToggleViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        _themeService.ThemeChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(KnobMargin));
            OnPropertyChanged(nameof(IconText));
            OnPropertyChanged(nameof(LabelText));
        };

        ToggleCommand = new RelayCommand(_ => _themeService.ToggleTheme());
    }

    public bool IsDarkTheme => _themeService.IsDark;
    public string KnobMargin => IsDarkTheme ? "34,3,3,3" : "3,3,34,3";
    public string IconText => IsDarkTheme ? "☾" : "☀";
    public string LabelText => IsDarkTheme ? "Night" : "Day";
    public ICommand ToggleCommand { get; }
}
