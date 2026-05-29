using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PCOptimizer.ViewModels;

namespace PCOptimizer.Views;

public partial class SwuabPageView : UserControl
{
    public SwuabPageView()
    {
        InitializeComponent();
    }

    private async void OnLoadConfigClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SwuabPageViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Swuab Config",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Text files")
                {
                    Patterns = ["*.txt"]
                }
            ]
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path))
        {
            await viewModel.LoadConfigAsync(path);
        }
    }
}
