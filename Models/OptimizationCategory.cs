using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using PCOptimizer.Utilities;

namespace PCOptimizer.Models;

public sealed class OptimizationCategory : ViewModelBase
{
    private bool _isSelected;
    private bool _isExpanded = true;
    private bool? _selectionState;
    private bool _isUpdating;

    public string Name { get; init; } = string.Empty;
    public ObservableCollection<OptimizationAction> Actions { get; } = new();

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                SelectionState = value;
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool? SelectionState
    {
        get => _selectionState;
        set
        {
            if (!SetProperty(ref _selectionState, value) || _isUpdating || value is null)
            {
                return;
            }

            SetAllActions(value.Value);
        }
    }

    public void AddAction(OptimizationAction action)
    {
        action.PropertyChanged += OnActionPropertyChanged;
        Actions.Add(action);
        RefreshSelectionState();
    }

    public void SetAllActions(bool isSelected)
    {
        _isUpdating = true;

        foreach (var action in Actions)
        {
            if (action.IsEnabled)
            {
                action.IsSelected = isSelected;
            }
        }

        _isUpdating = false;
        RefreshSelectionState();
    }

    public void RefreshSelectionState()
    {
        if (Actions.Count == 0)
        {
            _selectionState = false;
            _isSelected = false;
            OnPropertyChanged(nameof(SelectionState));
            OnPropertyChanged(nameof(IsSelected));
            return;
        }

        var selectable = Actions.Where(action => action.IsEnabled).ToList();
        var selectedCount = selectable.Count(action => action.IsSelected);
        bool? newState = selectedCount == 0 ? false : selectedCount == selectable.Count ? true : null;

        _selectionState = newState;
        _isSelected = newState == true;
        OnPropertyChanged(nameof(SelectionState));
        OnPropertyChanged(nameof(IsSelected));
    }

    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OptimizationAction.IsSelected) && !_isUpdating)
        {
            RefreshSelectionState();
        }
    }
}
