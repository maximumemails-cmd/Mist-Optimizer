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
    private string _searchText = string.Empty;
    private double _progress;
    private string _statusText = "Waiting";
    private string _emptyStateText = string.Empty;
    private bool _bulkUpdating;

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

        AllCategories = BuildCategories(actions);
        CategoryFilters = new ObservableCollection<string>(
            new[] { "All" }.Concat(AllCategories.Select(category => category.Name)).Distinct(StringComparer.OrdinalIgnoreCase));
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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
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
    public int TotalCount => Actions.Count;
    public int VisibleCount => FilteredCategories.Sum(category => category.Actions.Count);
    public int SelectedCount => Actions.Count(action => action.IsSelected);
    public int VisibleSelectedCount => FilteredCategories.SelectMany(category => category.Actions).Count(action => action.IsSelected);
    public int VisibleApplicableCount => FilteredCategories.SelectMany(category => category.Actions).Count(action => action.IsEnabled);
    public string CountDisplay => $"{SelectedCount} selected / {VisibleCount} visible / {TotalCount} total";
    public string SelectedCountDisplay => $"{SelectedCount} selected";

    public string EmptyStateText
    {
        get => _emptyStateText;
        private set => SetProperty(ref _emptyStateText, value);
    }

    private async Task ApplySelectedAsync()
    {
        var selected = Actions.Where(action => action.IsSelected && action.IsEnabled).ToList();
        StatusText = $"Applying {selected.Count} selected action(s)";
        Progress = 0;

        var restartNeeded = await _engine.ApplySelectedAsync(
            Actions,
            new Progress<double>(value => Progress = value),
            previewOnly: false);

        StatusText = BuildRunStatus("Apply", selected, restartNeeded);
        RefreshCounts();

        if (_restartPanel && restartNeeded)
        {
            RestartRequiredCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task PreviewSelectedAsync()
    {
        var selected = Actions.Where(action => action.IsSelected && action.IsEnabled).ToList();
        StatusText = $"Previewing {selected.Count} selected action(s)";
        Progress = 0;

        await _engine.ApplySelectedAsync(
            Actions,
            new Progress<double>(value => Progress = value),
            previewOnly: true);

        StatusText = BuildRunStatus("Preview", selected, restartNeeded: false);
        RefreshCounts();
    }

    private async Task RevertSelectedAsync()
    {
        var selected = Actions.Where(action => action.IsSelected && action.IsEnabled).ToList();
        StatusText = $"Reverting {selected.Count} selected action(s)";
        Progress = 0;

        await _engine.RevertSelectedAsync(
            Actions,
            new Progress<double>(value => Progress = value));

        StatusText = BuildRunStatus("Revert", selected, restartNeeded: false);
        RefreshCounts();
    }

    public void ToggleActionSelection(OptimizationAction action)
    {
        if (!action.IsEnabled)
        {
            StatusText = $"{action.Name} is unavailable and cannot be selected.";
            _logger.Warning($"{action.Name}: selection ignored because the optimisation is disabled or unavailable.");
            return;
        }

        action.ToggleSelection();
        StatusText = action.IsSelected
            ? $"{action.Name} selected"
            : $"{action.Name} deselected";
    }

    private void SetAllVisible(bool selected)
    {
        _bulkUpdating = true;

        foreach (var category in FilteredCategories)
        {
            category.SetAllActions(selected);
        }

        _bulkUpdating = false;
        RefreshFilteredCategories();
        var visibleApplicable = FilteredCategories.SelectMany(category => category.Actions).Count(action => action.IsEnabled);
        StatusText = selected
            ? $"{visibleApplicable} visible applicable action(s) selected"
            : $"{visibleApplicable} visible applicable action(s) deselected";
        RefreshCounts();
    }

    private void RefreshFilteredCategories()
    {
        FilteredCategories.Clear();

        foreach (var category in AllCategories)
        {
            if (SelectedFilter != "All" && category.Name != SelectedFilter)
            {
                continue;
            }

            var matchingActions = category.Actions.Where(MatchesSearch).ToList();

            if (matchingActions.Count == 0)
            {
                continue;
            }

            var filteredCategory = new OptimizationCategory { Name = category.Name };

            foreach (var action in matchingActions)
            {
                filteredCategory.Actions.Add(action);
            }

            filteredCategory.RefreshSelectionState();

            FilteredCategories.Add(filteredCategory);
        }

        EmptyStateText = FilteredCategories.Count == 0
            ? $"No {Title.ToLowerInvariant()} rows match this filter. Disabled or unsafe candidates remain visible in their matching categories."
            : string.Empty;
        RefreshCounts();
    }

    private ObservableCollection<OptimizationCategory> BuildCategories(IEnumerable<OptimizationAction> actions)
    {
        var categories = new ObservableCollection<OptimizationCategory>();

        foreach (var group in actions.GroupBy(action => string.IsNullOrWhiteSpace(action.Category) ? "Uncategorized" : action.Category).OrderBy(group => group.Key))
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
            if (!_bulkUpdating)
            {
                RefreshFilteredCategories();
            }

            RefreshCounts();
        }
    }

    private bool MatchesSearch(OptimizationAction action)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var query = SearchText.Trim();
        return action.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || action.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
            || action.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
            || action.Source.Contains(query, StringComparison.OrdinalIgnoreCase)
            || action.ImplementationStatus.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(VisibleCount));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(VisibleSelectedCount));
        OnPropertyChanged(nameof(VisibleApplicableCount));
        OnPropertyChanged(nameof(CountDisplay));
        OnPropertyChanged(nameof(SelectedCountDisplay));
    }

    private static string BuildRunStatus(string verb, IReadOnlyCollection<OptimizationAction> selected, bool restartNeeded)
    {
        if (selected.Count == 0)
        {
            return $"{verb} skipped: 0 selected.";
        }

        var completed = selected.Count(action => action.Status == OptimizationStatus.Completed);
        var skipped = selected.Count(action => action.Status is OptimizationStatus.Skipped or OptimizationStatus.NotImplemented);
        var failed = selected.Count(action => action.Status == OptimizationStatus.Failed);
        var suffix = restartNeeded ? " Restart required." : string.Empty;
        return $"{verb} complete: {completed} succeeded, {skipped} skipped, {failed} failed.{suffix}";
    }
}
