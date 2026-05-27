using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using PCOptimizer.Models;
using PCOptimizer.Services;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class LogPanelViewModel : ViewModelBase
{
    private readonly AppLogger _logger;

    public LogPanelViewModel(AppLogger logger)
    {
        _logger = logger;
        Logs = _logger.Entries;
        ClearCommand = new RelayCommand(_ => _logger.Clear());
        CopyCommand = new AsyncRelayCommand(_ => CopyAsync());
        SaveCommand = new RelayCommand(_ => _logger.Save());
    }

    public ObservableCollection<LogEntry> Logs { get; }
    public ICommand ClearCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand SaveCommand { get; }

    private async Task CopyAsync()
    {
        var text = _logger.Copy();

        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow.Clipboard: { } clipboard })
            {
                await clipboard.SetTextAsync(text);
                _logger.Info("Logs copied to clipboard.");
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Clipboard copy failed: {ex.Message}");
        }

        _logger.Warning("Clipboard is unavailable on this platform right now.");
    }
}
