using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PCOptimizer.Services;
using PCOptimizer.ViewModels;
using PCOptimizer.Views;

namespace PCOptimizer;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logger = new AppLogger();
            var settingsService = new SettingsService();
            var settings = settingsService.Load();
            var systemInfoService = new SystemInfoService();
            var catalogService = new OptimizationCatalogService(logger);
            var windowsCommandService = new WindowsCommandService(logger);
            var optimizerStateService = new OptimizerStateService();
            var optimizationEngine = new OptimizationEngine(logger, systemInfoService, windowsCommandService, optimizerStateService);
            var protectedProcessHelper = new ProtectedProcessHelper();
            var processService = new ProcessService(protectedProcessHelper);
            var ramCleanerService = new RamCleanerService(logger, processService, systemInfoService);
            _ = new BackupService(logger, settingsService);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    logger,
                    settingsService,
                    catalogService,
                    optimizationEngine,
                    ramCleanerService,
                    systemInfoService,
                    settings)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
