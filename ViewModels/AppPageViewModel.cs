using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class AppPageViewModel : ViewModelBase
{
    private bool _isCurrent;

    public AppPageViewModel(string title, string subtitle, object content)
    {
        Title = title;
        Subtitle = subtitle;
        Content = content;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public object Content { get; }

    public bool IsCurrent
    {
        get => _isCurrent;
        set => SetProperty(ref _isCurrent, value);
    }
}
