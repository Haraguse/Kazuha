using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;

namespace Luminalium.App.ViewModels;

/// <summary>
/// View model for the "存储、档案和备份" settings page. Drives profile management,
/// configuration backups and storage usage reporting through the supporting services.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "PageTitle is bound to the UI as an instance member so compiled bindings resolve consistently.")]
public sealed partial class StorageSettingsViewModel : ObservableObject
{
    private readonly ProfileService? _profileService;
    private readonly ConfigurationBackupService? _backupService;
    private readonly StorageService? _storageService;
    private readonly SettingsCoordinator? _settingsCoordinator;

    public StorageSettingsViewModel(
        ProfileService? profileService = null,
        ConfigurationBackupService? backupService = null,
        StorageService? storageService = null,
        SettingsCoordinator? settingsCoordinator = null)
    {
        _profileService = profileService;
        _backupService = backupService;
        _storageService = storageService;
        _settingsCoordinator = settingsCoordinator;

        RefreshProfiles();
        RefreshBackups();
        RefreshStorage();

        CreateProfileCommand = new RelayCommand(CreateProfile);
        SwitchProfileCommand = new RelayCommand(SwitchProfile, () => ProfileServiceAvailable);
        RenameProfileCommand = new RelayCommand(RenameProfile, () => ProfileServiceAvailable);
        DeleteProfileCommand = new RelayCommand(DeleteProfile, () => ProfileServiceAvailable);
        CreateBackupCommand = new RelayCommand(CreateBackup);
        RenameBackupCommand = new RelayCommand(RenameBackup);
        DeleteBackupCommand = new RelayCommand(DeleteBackup);
        RestoreBackupCommand = new RelayCommand(RestoreBackup);
        RefreshStorageCommand = new RelayCommand(RefreshStorage);
        CleanUpdateCacheCommand = new RelayCommand(CleanUpdateCache);
        CleanBackupsCommand = new RelayCommand(CleanBackups);
    }

    private bool ProfileServiceAvailable => _profileService is not null;
    private bool BackupServiceAvailable => _backupService is not null;
    private bool StorageServiceAvailable => _storageService is not null;

    public string PageTitle => "存储、档案和备份";

    [ObservableProperty]
    private string activeProfileName = "default";

    [ObservableProperty]
    private IReadOnlyList<string> profiles = [];

    [ObservableProperty]
    private string selectedProfile = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<string> backups = [];

    [ObservableProperty]
    private string selectedBackup = string.Empty;

    [ObservableProperty]
    private string newProfileName = string.Empty;

    [ObservableProperty]
    private string newBackupName = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasStatus;

    [ObservableProperty]
    private string settingsDirectorySize = "—";

    [ObservableProperty]
    private string backupsDirectorySize = "—";

    [ObservableProperty]
    private string updateCacheDirectorySize = "—";

    [ObservableProperty]
    private string logsDirectorySize = "—";

    [ObservableProperty]
    private string settingsDirectoryPath = string.Empty;

    [ObservableProperty]
    private string backupsDirectoryPath = string.Empty;

    [ObservableProperty]
    private string updateCacheDirectoryPath = string.Empty;

    [ObservableProperty]
    private string logsDirectoryPath = string.Empty;

    public IRelayCommand CreateProfileCommand { get; }
    public IRelayCommand SwitchProfileCommand { get; }
    public IRelayCommand RenameProfileCommand { get; }
    public IRelayCommand DeleteProfileCommand { get; }
    public IRelayCommand CreateBackupCommand { get; }
    public IRelayCommand RenameBackupCommand { get; }
    public IRelayCommand DeleteBackupCommand { get; }
    public IRelayCommand RestoreBackupCommand { get; }
    public IRelayCommand RefreshStorageCommand { get; }
    public IRelayCommand CleanUpdateCacheCommand { get; }
    public IRelayCommand CleanBackupsCommand { get; }

    private void CreateProfile()
    {
        if (_profileService is null)
        {
            SetStatus("档案服务不可用。", isSuccess: false);
            return;
        }

        var result = _profileService.CreateProfile(NewProfileName);
        SetStatus(result.IsSuccess ? $"已创建档案“{NewProfileName}”。" : result.ErrorMessage!, result.IsSuccess);
        if (result.IsSuccess)
        {
            NewProfileName = string.Empty;
        }

        RefreshProfiles();
    }

    private async void SwitchProfile()
    {
        if (_profileService is null)
        {
            SetStatus("档案服务不可用。", isSuccess: false);
            return;
        }

        if (string.IsNullOrEmpty(SelectedProfile))
        {
            SetStatus("请先选择一个档案。", isSuccess: false);
            return;
        }

        if (_settingsCoordinator is not null)
        {
            var reloaded = await _settingsCoordinator.ReloadFromProfileAsync(SelectedProfile);
            if (!reloaded)
            {
                SetStatus("切换档案失败：无法重新加载配置。", isSuccess: false);
                RefreshProfiles();
                return;
            }
        }
        else
        {
            var fallback = _profileService.SwitchProfile(SelectedProfile);
            if (!fallback.IsSuccess)
            {
                SetStatus(fallback.ErrorMessage!, isSuccess: false);
                RefreshProfiles();
                return;
            }
        }

        SetStatus($"已切换到档案“{SelectedProfile}”。", isSuccess: true);
        RefreshProfiles();
    }

    private void RenameProfile()
    {
        if (_profileService is null)
        {
            SetStatus("档案服务不可用。", isSuccess: false);
            return;
        }

        if (string.IsNullOrEmpty(SelectedProfile))
        {
            SetStatus("请先选择一个要重命名的档案。", isSuccess: false);
            return;
        }

        var result = _profileService.RenameProfile(SelectedProfile, NewProfileName);
        SetStatus(result.IsSuccess ? $"已重命名档案为“{NewProfileName}”。" : result.ErrorMessage!, result.IsSuccess);
        if (result.IsSuccess)
        {
            NewProfileName = string.Empty;
        }

        RefreshProfiles();
    }

    private void DeleteProfile()
    {
        if (_profileService is null)
        {
            SetStatus("档案服务不可用。", isSuccess: false);
            return;
        }

        if (string.IsNullOrEmpty(SelectedProfile))
        {
            SetStatus("请先选择一个要删除的档案。", isSuccess: false);
            return;
        }

        var result = _profileService.DeleteProfile(SelectedProfile);
        SetStatus(result.IsSuccess ? $"已删除档案“{SelectedProfile}”。" : result.ErrorMessage!, result.IsSuccess);
        RefreshProfiles();
    }

    private void CreateBackup()
    {
        if (_backupService is null)
        {
            SetStatus("备份服务不可用。", isSuccess: false);
            return;
        }

        var result = _backupService.CreateBackup(NewBackupName);
        SetStatus(result.IsSuccess ? "已创建备份。" : result.ErrorMessage!, result.IsSuccess);
        if (result.IsSuccess)
        {
            NewBackupName = string.Empty;
        }

        RefreshBackups();
        RefreshStorage();
    }

    private void RenameBackup()
    {
        if (_backupService is null)
        {
            SetStatus("备份服务不可用。", isSuccess: false);
            return;
        }

        if (string.IsNullOrEmpty(SelectedBackup))
        {
            SetStatus("请先选择一个要重命名的备份。", isSuccess: false);
            return;
        }

        var result = _backupService.RenameBackup(SelectedBackup, NewBackupName);
        SetStatus(result.IsSuccess ? "已重命名备份。" : result.ErrorMessage!, result.IsSuccess);
        if (result.IsSuccess)
        {
            NewBackupName = string.Empty;
        }

        RefreshBackups();
    }

    private void DeleteBackup()
    {
        if (_backupService is null)
        {
            SetStatus("备份服务不可用。", isSuccess: false);
            return;
        }

        if (string.IsNullOrEmpty(SelectedBackup))
        {
            SetStatus("请先选择一个要删除的备份。", isSuccess: false);
            return;
        }

        var result = _backupService.DeleteBackup(SelectedBackup);
        SetStatus(result.IsSuccess ? $"已删除备份“{SelectedBackup}”。" : result.ErrorMessage!, result.IsSuccess);
        RefreshBackups();
        RefreshStorage();
    }

    private void RestoreBackup()
    {
        if (_backupService is null)
        {
            SetStatus("备份服务不可用。", isSuccess: false);
            return;
        }

        if (string.IsNullOrEmpty(SelectedBackup))
        {
            SetStatus("请先选择一个要恢复的备份。", isSuccess: false);
            return;
        }

        var targetProfile = ActiveProfileName == "default" ? null : ActiveProfileName;
        var result = _backupService.RestoreBackup(SelectedBackup, targetProfile);
        SetStatus(result.IsSuccess ? $"已从备份“{SelectedBackup}”恢复当前配置。" : result.ErrorMessage!, result.IsSuccess);
    }

    private void RefreshStorage()
    {
        if (_storageService is null)
        {
            return;
        }

        var info = _storageService.GetStorageInfo();
        SettingsDirectoryPath = _storageService.SettingsDirectoryPath;
        BackupsDirectoryPath = _storageService.BackupsDirectoryPath;
        UpdateCacheDirectoryPath = _storageService.UpdateCacheDirectoryPath;
        LogsDirectoryPath = _storageService.LogsDirectoryPath;
        SettingsDirectorySize = FormatSize(info.SettingsDirectorySize);
        BackupsDirectorySize = FormatSize(info.BackupsDirectorySize);
        UpdateCacheDirectorySize = FormatSize(info.UpdateCacheDirectorySize);
        LogsDirectorySize = FormatSize(info.LogsDirectorySize);
    }

    private void CleanUpdateCache()
    {
        if (_storageService is null)
        {
            SetStatus("存储服务不可用。", isSuccess: false);
            return;
        }

        var result = _storageService.CleanUpdateCache();
        SetStatus(result.IsSuccess ? "已清理更新缓存。" : result.ErrorMessage!, result.IsSuccess);
        RefreshStorage();
    }

    private void CleanBackups()
    {
        if (_storageService is null)
        {
            SetStatus("存储服务不可用。", isSuccess: false);
            return;
        }

        var result = _storageService.CleanBackups();
        SetStatus(result.IsSuccess ? "已清理全部备份。" : result.ErrorMessage!, result.IsSuccess);
        RefreshBackups();
        RefreshStorage();
    }

    private void RefreshProfiles()
    {
        if (_profileService is null)
        {
            Profiles = [];
            SelectedProfile = string.Empty;
            ActiveProfileName = "default";
            return;
        }

        Profiles = _profileService.ListProfiles();
        ActiveProfileName = _profileService.GetActiveProfileName() ?? "default";
        if (string.IsNullOrEmpty(SelectedProfile) || !Profiles.Contains(SelectedProfile))
        {
            SelectedProfile = ActiveProfileName;
        }
    }

    private void RefreshBackups()
    {
        if (_backupService is null)
        {
            Backups = [];
            SelectedBackup = string.Empty;
            return;
        }

        Backups = _backupService.ListBackups();
        if (string.IsNullOrEmpty(SelectedBackup) || !Backups.Contains(SelectedBackup))
        {
            SelectedBackup = Backups.Count > 0 ? Backups[0] : string.Empty;
        }
    }

    private void SetStatus(string message, bool isSuccess)
    {
        StatusMessage = message;
        HasStatus = true;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        var units = new[] { "KB", "MB", "GB" };
        var size = (double)bytes;
        var unit = "B";
        for (var i = 0; i < units.Length && size >= 1024; i++)
        {
            size /= 1024;
            unit = units[i];
        }

        return $"{size.ToString("0.0", CultureInfo.InvariantCulture)} {unit}";
    }
}