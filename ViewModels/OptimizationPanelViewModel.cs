using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PCOptimizer.Models;
using PCOptimizer.Services;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class OptimizationPanelViewModel : ViewModelBase
{
    private readonly OptimizationEngine _engine;
    private readonly AppLogger _logger;
    private readonly bool _restartPanel;
    private string _selectedFilter = "All";
    private double _progress;
    private string _statusText = "Waiting";
    private string _emptyStateText = string.Empty;

    public OptimizationPanelViewModel(
        string title,
        string subtitle,
        bool restartPanel,
        IEnumerable<OptimizationAction> actions,
        OptimizationEngine engine,
        AppLogger logger)
    {
        Title = title;
        Subtitle = subtitle;
        _restartPanel = restartPanel;
        _engine = engine;
        _logger = logger;

        CategoryFilters = new ObservableCollection<string>
        {
            "All",
            "Network",
            "Hardware",
            "Gaming",
            "Startup",
            "Services",
            "Privacy",
            "Storage",
            "Power",
            "Visual Effects",
            "Drivers / Updates",
            "Restore / Backups",
            "Advanced"
        };

        AllCategories = BuildCategories(actions);
        FilteredCategories = new ObservableCollection<OptimizationCategory>();
        RefreshFilteredCategories();

        SelectAllCommand = new RelayCommand(_ => SetAllVisible(true));
        DeselectAllCommand = new RelayCommand(_ => SetAllVisible(false));
        ApplySelectedCommand = new AsyncRelayCommand(_ => ApplySelectedAsync());
        PreviewSelectedCommand = new AsyncRelayCommand(_ => PreviewSelectedAsync());
        RevertSelectedCommand = new AsyncRelayCommand(_ => RevertSelectedAsync());
    }

    public event EventHandler<double>? ProgressChanged;
    public event EventHandler? RestartRequiredCompleted;

    public string Title { get; }
    public string Subtitle { get; }
    public ObservableCollection<string> CategoryFilters { get; }
    public ObservableCollection<OptimizationCategory> AllCategories { get; }
    public ObservableCollection<OptimizationCategory> FilteredCategories { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand ApplySelectedCommand { get; }
    public ICommand PreviewSelectedCommand { get; }
    public ICommand RevertSelectedCommand { get; }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                RefreshFilteredCategories();
            }
        }
    }

    public double Progress
    {
        get => _progress;
        set
        {
            if (SetProperty(ref _progress, value))
            {
                ProgressChanged?.Invoke(this, value);
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public IReadOnlyList<OptimizationAction> Actions => AllCategories.SelectMany(category => category.Actions).ToList();

    public string EmptyStateText
    {
        get => _emptyStateText;
        private set => SetProperty(ref _emptyStateText, value);
    }

    private async Task ApplySelectedAsync()
    {
        StatusText = "Applying selected actions";
        Progress = 0;

        var restartNeeded = await _engine.ApplySelectedAsync(
            Actions,
            new Progress<double>(value => Progress = value),
            previewOnly: false);

        StatusText = restartNeeded ? "Completed. Restart required." : "Completed safely.";

        if (_restartPanel && restartNeeded)
        {
            RestartRequiredCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task PreviewSelectedAsync()
    {
        StatusText = "Previewing selected changes";
        Progress = 0;

        await _engine.ApplySelectedAsync(
            Actions,
            new Progress<double>(value => Progress = value),
            previewOnly: true);

        StatusText = "Preview complete. No changes were made.";
    }

    private async Task RevertSelectedAsync()
    {
        StatusText = "Reverting selected actions";
        Progress = 0;

        await _engine.RevertSelectedAsync(
            Actions,
            new Progress<double>(value => Progress = value));

        StatusText = "Revert complete.";
    }

    private void SetAllVisible(bool selected)
    {
        foreach (var category in FilteredCategories)
        {
            category.SetAllActions(selected);
        }

        StatusText = selected ? "Visible actions selected" : "Visible actions deselected";
    }

    private void RefreshFilteredCategories()
    {
        FilteredCategories.Clear();

        foreach (var category in AllCategories)
        {
            if (SelectedFilter == "All" || category.Name == SelectedFilter)
            {
                FilteredCategories.Add(category);
            }
        }

        EmptyStateText = FilteredCategories.Count == 0
            ? $"No {Title.ToLowerInvariant()} rows match this filter. Disabled or unsafe candidates remain visible in their matching categories."
            : string.Empty;
    }

    private ObservableCollection<OptimizationCategory> BuildCategories(IEnumerable<OptimizationAction> actions)
    {
        var categories = new ObservableCollection<OptimizationCategory>();

        foreach (var group in actions.GroupBy(action => action.Category).OrderBy(group => group.Key))
        {
            var category = new OptimizationCategory { Name = group.Key };

            foreach (var action in group.OrderBy(action => action.Name))
            {
                action.PropertyChanged += OnActionPropertyChanged;
                category.AddAction(action);
            }

            categories.Add(category);
        }

        return categories;
    }

    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is OptimizationAction action && e.PropertyName == nameof(OptimizationAction.IsSelected))
        {
            _logger.Info($"{action.Name}: {(action.IsSelected ? "selected" : "deselected")}.");
        }
    }
}
