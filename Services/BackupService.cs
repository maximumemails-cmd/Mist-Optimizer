using System;
using System.Collections.Generic;
using System.IO;

namespace PCOptimizer.Services;

public sealed class BackupService
{
    private readonly AppLogger _logger;
    private readonly SettingsService _settingsService;

    public BackupService(AppLogger logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    public void CreateRestorePoint()
    {
        _logger.Warning("Create Restore Point is placeholder-only. No restore point was created.");
    }

    public void CreateRegistryBackup()
    {
        _logger.Warning("Create Registry Backup is placeholder-only. No registry data was read or changed.");
    }

    public string CreateAppSettingsBackup()
    {
        var backupDirectory = Path.Combine(_settingsService.SettingsDirectory, "Backups");
        Directory.CreateDirectory(backupDirectory);

        var backupPath = Path.Combine(backupDirectory, $"settings-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");

        if (File.Exists(_settingsService.SettingsPath))
        {
            File.Copy(_settingsService.SettingsPath, backupPath, overwrite: false);
            _logger.Info($"App settings backup created at {backupPath}");
        }
        else
        {
            File.WriteAllText(backupPath, "{}");
            _logger.Warning($"Settings file did not exist. Empty backup created at {backupPath}");
        }

        return backupPath;
    }

    public IReadOnlyList<string> ListBackups()
    {
        var backupDirectory = Path.Combine(_settingsService.SettingsDirectory, "Backups");
        Directory.CreateDirectory(backupDirectory);

        var backups = Directory.GetFiles(backupDirectory, "*.json");
        _logger.Info($"Found {backups.Length} app settings backup(s).");
        return backups;
    }

    public void RestoreBackup()
    {
        _logger.Warning("Restore Backup is placeholder-only. No backup was restored.");
    }
}
