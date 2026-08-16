using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Luminalium.App.ViewModels;

/// <summary>
/// Placeholder view model for the redesigned "个性化设置" (Personalization) page.
/// Exposes UI-bound properties for appearance, splash screen and overlay themes.
/// Real persistence and system integration will be wired up later.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Label/description properties are bound to the UI as instance members so design-time and runtime bindings resolve consistently.")]
public sealed partial class PersonalizationSettingsViewModel : ObservableObject
{
    private readonly SettingsCoordinator? _coordinator;
    private bool _initializing;

    public PersonalizationSettingsViewModel(LuminaliumConfig? config = null, SettingsCoordinator? coordinator = null)
    {
        _coordinator = coordinator;
        var source = config ?? LuminaliumConfig.CreateDefault();
        _initializing = true;
        selectedThemeModeIndex = source.Appearance.ThemeMode switch
        {
            ThemeMode.Light => 0,
            ThemeMode.Dark => 1,
            _ => 2,
        };
        selectedAccentIndex = Enumerable.Range(0, AccentOptionsList.Count)
            .FirstOrDefault(index => AccentOptionsList[index].PrimaryColor.Equals(source.Appearance.AccentColor, StringComparison.OrdinalIgnoreCase), 0);
        if (selectedAccentIndex < 0) selectedAccentIndex = 0;
        selectedSplashModeIndex = (int)source.General.SplashMode;
        showDetailedSplash = source.General.ShowDetailedSplash;
        selectedSplashStyleIndex = Math.Max(0, SplashStyleOptionsList
            .Select((option, index) => (option, index))
            .FirstOrDefault(item => item.option.StyleId == source.General.SplashStyle).index);
        _initializing = false;
    }

    private static readonly IReadOnlyList<AccentOption> AccentOptionsList =
    [
        new("默认", "#3275F5", "#E8F0FE", "#FFFFFF"),
        new("蓝色", "#0078D4", "#DEECF9", "#FFFFFF"),
        new("绿色", "#107C10", "#DFF6DD", "#FFFFFF"),
        new("橙色", "#CA5010", "#FCE4D6", "#FFFFFF"),
        new("玫红", "#E74856", "#FDE7E9", "#FFFFFF"),
    ];

    private static readonly IReadOnlyList<SplashStyleOption> SplashStyleOptionsList =
    [
        new("default", "默认", "avares://Luminalium/Assets/Splash/default_preview.png"),
        new("momokan_1_4", "1.4 - Momokan", "avares://Luminalium/Assets/Splash/momokan_preview.png"),
        new("nina_iseri_1_2", "nina_iseri_1_2", "avares://Luminalium/Assets/Splash/nina_preview.png"),
    ];
    [ObservableProperty]
    private int selectedTabIndex;

    [ObservableProperty]
    private int selectedThemeModeIndex;

    [ObservableProperty]
    private int selectedAccentIndex;

    [ObservableProperty]
    private int selectedSplashModeIndex;

    [ObservableProperty]
    private bool showDetailedSplash;

    [ObservableProperty]
    private int selectedSplashStyleIndex;

    [ObservableProperty]
    private int selectedOverlayThemeIndex;

    public IReadOnlyList<string> Tabs { get; } = ["外观", "启动画面", "Overlay 主题"];

    public IReadOnlyList<string> ThemeModes { get; } = ["浅色", "深色", "跟随系统"];

    public IReadOnlyList<string> SplashModes { get; } = ["每次都显示", "从不显示", "开机自启时隐藏", "指定时段隐藏"];

    public IReadOnlyList<AccentOption> AccentOptions { get; } =
    [
        new("默认", "#3275F5", "#E8F0FE", "#FFFFFF"),
        new("RinLit", "#7C4DFF", "#EDE7F6", "#FFFFFF"),
        new("红色", "#EA4335", "#FCE8E6", "#FFFFFF"),
        new("灰色", "#5F6368", "#E8EAED", "#FFFFFF"),
        new("绿色", "#34A853", "#E6F4EA", "#FFFFFF"),
        new("橙色", "#FB8C00", "#FEF3E8", "#FFFFFF"),
        new("马年大吉", "#D93025", "#FCE8E6", "#FFF8E1"),
        new("OSU!", "#FF66AA", "#FCE4EC", "#FFFFFF"),
        new("无刺有刺", "#1E88E5", "#E3F2FD", "#FFFFFF"),
        new("动态取色", "#78909C", "#ECEFF1", "#FFFFFF"),
        new("自定义", "#9E9E9E", "#F5F5F5", "#FFFFFF"),
    ];

    public IReadOnlyList<SplashStyleOption> SplashStyleOptions { get; } =
    [
        new("default", "默认", "avares://Luminalium/Assets/Splash/default_preview.png"),
        new("momokan_1_4", "1.4 - Momokan", "avares://Luminalium/Assets/Splash/momokan_preview.png"),
        new("nina_iseri_1_2", "nina_iseri_1_2", "avares://Luminalium/Assets/Splash/nina_preview.png"),
    ];

    public IReadOnlyList<OverlayThemeOption> OverlayThemeOptions { get; } =
    [
        new("default", "默认主题", "avares://Luminalium/Assets/Splash/default_preview.png"),
    ];

    public string PageTitle => "个性化设置";

    public string AppearanceModeLabel => "外观模式";

    public string AppearanceModeDescription => "调整应用程序的显示主题";

    public string AccentSchemeLabel => "配色方案";

    public string AccentSchemeDescription => "选择配色方案，影响设置页与工具栏外观";

    public string SplashScreenLabel => "启动画面";

    public string SplashScreenDescription => "控制启动画面的显示时机；可对开机自启或指定时段单独隐藏";

    public string ShowDetailedSplashLabel => "启动画面显示详细启动进度";

    public string ShowDetailedSplashDescription => "开启后在启动画面显示字体、插件等详细加载进度文本";

    public string SplashStyleLabel => "启动画面样式";

    public string SplashStyleDescription => "选择启动画面的显示风格";

    public string OverlayThemeLabel => "Overlay 主题";

    public string OverlayThemeDescription => "自定义 Overlay 的 HTML/CSS/JS";

    public string StatusOnText => "开";

    public string StatusOffText => "关";

    partial void OnSelectedTabIndexChanged(int value) => OnPropertyChanged(nameof(SelectedTabIndex));

    partial void OnSelectedThemeModeIndexChanged(int value) => Persist(c => c.Appearance.ThemeMode = value switch
    {
        1 => ThemeMode.Dark,
        2 => ThemeMode.Auto,
        _ => ThemeMode.Light,
    });

    partial void OnSelectedAccentIndexChanged(int value)
    {
        if (value >= 0 && value < AccentOptionsList.Count)
        {
            Persist(c => c.Appearance.AccentColor = AccentOptionsList[value].PrimaryColor);
        }
    }

    partial void OnSelectedSplashModeIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(SplashMode), value)) Persist(c => c.General.SplashMode = (SplashMode)value);
    }

    partial void OnShowDetailedSplashChanged(bool value) => Persist(c => c.General.ShowDetailedSplash = value);

    partial void OnSelectedSplashStyleIndexChanged(int value)
    {
        if (value >= 0 && value < SplashStyleOptionsList.Count)
        {
            Persist(c => c.General.SplashStyle = SplashStyleOptionsList[value].StyleId);
        }
    }

    partial void OnSelectedOverlayThemeIndexChanged(int value)
    {
        if (value >= 0 && value < OverlayThemeOptions.Count)
        {
            Persist(c => c.Appearance.OverlayTheme = OverlayThemeOptions[value].ThemeId);
        }
    }

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null) _ = _coordinator.UpdateAsync(update);
    }
}

public sealed record AccentOption(string Name, string PrimaryColor, string SecondaryColor, string SurfaceColor);

public sealed record SplashStyleOption(string StyleId, string Name, string PreviewUri);

public sealed record OverlayThemeOption(string ThemeId, string Name, string PreviewUri);
