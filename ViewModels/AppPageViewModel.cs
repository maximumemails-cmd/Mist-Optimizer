using System;
using System.Windows.Input;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class AppPageViewModel : ViewModelBase
{
    private bool _isCurrent;

    public AppPageViewModel(string title, string subtitle, object content, Action<AppPageViewModel>? activate = null)
    {
        Title = title;
        Subtitle = subtitle;
        Content = content;
        ActivateCommand = new RelayCommand(_ => activate?.Invoke(this));
    }

    public string Title { get; }
    public string Subtitle { get; }
    public object Content { get; }
    public ICommand ActivateCommand { get; }

    public bool IsCurrent
    {
        get => _isCurrent;
        set => SetProperty(ref _isCurrent, value);
    }
}
