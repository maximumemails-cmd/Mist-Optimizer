using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using PCOptimizer.Models;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class SwuabSettingViewModel : ViewModelBase
{
    private bool _isSelected = true;
    private double _numericValue;
    private bool _boolValue;
    private string _selectedOption = string.Empty;
    private string _statusText = "Not applied";
    private string _currentWindowsValue = "Unknown";

    public SwuabSettingViewModel(
        string key,
        string title,
        string section,
        string description,
        SwuabSettingKind kind,
        double numericValue = 0,
        int minimum = 0,
        int maximum = 0,
        bool boolValue = false,
        string? selectedOption = null,
        IEnumerable<string>? options = null)
    {
        Key = key;
        Title = title;
        Section = section;
        Description = description;
        Kind = kind;
        Minimum = minimum;
        Maximum = maximum;
        _numericValue = numericValue;
        _boolValue = boolValue;
        Options = new ObservableCollection<string>(options ?? []);
        _selectedOption = selectedOption ?? Options.FirstOrDefault() ?? string.Empty;
    }

    public string Key { get; }
    public string Title { get; }
    public string Section { get; }
    public string Description { get; }
    public SwuabSettingKind Kind { get; }
    public int Minimum { get; }
    public int Maximum { get; }
    public ObservableCollection<string> Options { get; }
    public bool IsSlider => Kind == SwuabSettingKind.Slider;
    public bool IsToggle => Kind == SwuabSettingKind.Toggle;
    public bool IsDropdown => Kind == SwuabSettingKind.Dropdown;
    public string RangeDisplay => IsSlider ? $"{Minimum} - {Maximum}" : string.Empty;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public double NumericValue
    {
        get => _numericValue;
        set
        {
            var rounded = Math.Round(value);
            if (SetProperty(ref _numericValue, rounded))
            {
                OnPropertyChanged(nameof(ValueDisplay));
                OnPropertyChanged(nameof(ExportValue));
            }
        }
    }

    public bool BoolValue
    {
        get => _boolValue;
        set
        {
            if (SetProperty(ref _boolValue, value))
            {
                OnPropertyChanged(nameof(ValueDisplay));
                OnPropertyChanged(nameof(ExportValue));
            }
        }
    }

    public string SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (SetProperty(ref _selectedOption, value))
            {
                OnPropertyChanged(nameof(ValueDisplay));
                OnPropertyChanged(nameof(ExportValue));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string CurrentWindowsValue
    {
        get => _currentWindowsValue;
        set => SetProperty(ref _currentWindowsValue, value);
    }

    public string ValueDisplay
    {
        get
        {
            if (IsToggle)
            {
                return BoolValue ? "On" : "Off";
            }

            if (IsDropdown)
            {
                return SelectedOption;
            }

            return ((int)NumericValue).ToString(CultureInfo.InvariantCulture);
        }
    }

    public string ExportValue => IsSlider
        ? ((int)NumericValue).ToString(CultureInfo.InvariantCulture)
        : IsToggle
            ? BoolValue.ToString().ToLowerInvariant()
            : SelectedOption;

    public bool TrySetFromText(string value, out string error)
    {
        error = string.Empty;

        if (IsSlider)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ||
                number < Minimum ||
                number > Maximum)
            {
                error = $"{Title} must be a number from {Minimum} to {Maximum}.";
                return false;
            }

            NumericValue = number;
            return true;
        }

        if (IsToggle)
        {
            if (!bool.TryParse(value, out var flag))
            {
                error = $"{Title} must be true or false.";
                return false;
            }

            BoolValue = flag;
            return true;
        }

        var match = Options.FirstOrDefault(option => string.Equals(option, value, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            error = $"{Title} must be one of: {string.Join(", ", Options)}.";
            return false;
        }

        SelectedOption = match;
        return true;
    }
}
