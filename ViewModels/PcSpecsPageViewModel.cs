using System.Windows.Input;
using PCOptimizer.Models;
using PCOptimizer.Services;
using PCOptimizer.Utilities;

namespace PCOptimizer.ViewModels;

public sealed class PcSpecsPageViewModel : ViewModelBase
{
    private readonly SystemInfoService _systemInfoService;
    private SystemSpecs _specs = new();

    public PcSpecsPageViewModel(SystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;
        RefreshCommand = new RelayCommand(_ => Refresh());
        Refresh();
    }

    public ICommand RefreshCommand { get; }

    public SystemSpecs Specs
    {
        get => _specs;
        private set => SetProperty(ref _specs, value);
    }

    public void Refresh()
    {
        Specs = _systemInfoService.GetSystemSpecs();
    }
}
