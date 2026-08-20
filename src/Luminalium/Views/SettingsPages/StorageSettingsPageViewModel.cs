using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.Services.Config;

namespace Luminalium.Views.SettingsPages;

public sealed class StorageSettingsPageViewModel : ObservableObject
{
    private readonly MainConfigHandler _handler;

    public StorageSettingsPageViewModel(MainConfigHandler handler)
    {
        _handler = handler;
        OpenDirectoryCommand = new RelayCommand(OpenDirectory);
        ReloadCommand = new RelayCommand(handler.Reload);
        DeleteCommand = new RelayCommand(() =>
        {
            handler.Delete();
            handler.Reload();
        });
    }

    public string ConfigFilePath => _handler.Data.ConfigFilePath;
    public string ConfigDirectoryPath => Path.GetDirectoryName(ConfigFilePath) ?? string.Empty;
    public string SaveDescription => "配置项修改后会自动保存到本地文件。";
    public IRelayCommand OpenDirectoryCommand { get; }
    public IRelayCommand ReloadCommand { get; }
    public IRelayCommand DeleteCommand { get; }

    private void OpenDirectory()
    {
        var directory = ConfigDirectoryPath;
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo { FileName = directory, UseShellExecute = true });
    }
}
