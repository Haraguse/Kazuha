using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Services.Config;

namespace Luminalium.Views.SettingsPages;

public sealed class AboutSettingsPageViewModel(MainConfigHandler handler) : ObservableObject
{
    public string ProductName => "Luminalium";
    public string VersionDisplay => "开发版";
    public string Build => "新壳";
    public string Platform => $"{Environment.OSVersion.Platform} / {Environment.OSVersion.Version}";
    public string Copyright => "© 2026 SECTL";
    public string ProjectUrl => "https://github.com/SECTL/Luminalium";
    public string ThirdPartyNotices => "Avalonia UI（MIT License）\nCommunityToolkit.Mvvm（MIT License）\nFluentAvalonia（MIT License）\nSkiaSharp（MIT License）\nDotNetCampus.AvaloniaInkCanvas（MIT License）";
    public string ConfigPath => handler.Data.ConfigFilePath;
    public bool AutoCheckUpdates { get => handler.Data.AutoCheckUpdates; set => handler.Data.AutoCheckUpdates = value; }
    public int UpdateSourceIndex { get => handler.Data.UpdateSource; set => handler.Data.UpdateSource = value; }
}
