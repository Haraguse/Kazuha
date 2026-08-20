using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Luminalium.Models.Config;

public sealed partial class MainConfigModel : ConfigBase
{
    [JsonIgnore]
    public override string ConfigFilePath => Utils.GetFilePath("config.json");

    [ObservableProperty] private string language = "zh-CN";

    public int LanguageIndex
    {
        get => Language switch
        {
            "zh-TW" => 1,
            "ja-JP" => 2,
            "en-US" => 3,
            "ug-CN" => 4,
            "yue-HK" => 5,
            _ => 0,
        };
        set
        {
            Language = value switch
            {
                1 => "zh-TW",
                2 => "ja-JP",
                3 => "en-US",
                4 => "ug-CN",
                5 => "yue-HK",
                _ => "zh-CN",
            };
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> LanguageOptions { get; } =
    [
        "简体中文",
        "繁體中文",
        "日本語",
        "English",
        "维吾尔语",
        "粤语",
    ];
    [ObservableProperty] private bool runAtStartup;
    [ObservableProperty] private bool autoShowOverlay = true;
    [ObservableProperty] private bool disableAnimations;
    [ObservableProperty] private bool registerUrlProtocol;
    [ObservableProperty] private bool useNativeTitleBar;
    [ObservableProperty] private int crashHandlerMode;
    [ObservableProperty] private bool crashHandlerEnabled;
    [ObservableProperty] private int pageRateLimit = 2;
    [ObservableProperty] private bool passwordProtectionEnabled;
    [ObservableProperty] private bool compatibilityMode;

    [ObservableProperty] private string theme = "System";

    public int ThemeIndex
    {
        get => Theme switch
        {
            "Dark" => 1,
            "System" => 2,
            _ => 0,
        };
        set
        {
            Theme = value switch { 1 => "Dark", 2 => "System", _ => "Light" };
            OnPropertyChanged();
        }
    }
    [ObservableProperty] private string accentColor = "#3275F5";
    [ObservableProperty] private string fontFamily = string.Empty;
    [ObservableProperty] private bool customFont;
    [ObservableProperty] private int fontWeight;
    [ObservableProperty] private string previewSample = string.Empty;
    [ObservableProperty] private string languageFontOverrides = string.Empty;
    [ObservableProperty] private int splashMode;
    [ObservableProperty] private bool showDetailedSplash;
    [ObservableProperty] private int splashStyle;
    [ObservableProperty] private int overlayTheme;

    [ObservableProperty] private bool showClear = true;
    [ObservableProperty] private bool showSpotlight = true;
    [ObservableProperty] private bool showBoard = true;
    [ObservableProperty] private bool showTimer = true;
    [ObservableProperty] private bool showToolbarText;
    [ObservableProperty] private bool showTooltips = true;
    [ObservableProperty] private int clearMode;
    [ObservableProperty] private double toolbarOpacity = 1;
    [ObservableProperty] private bool syncOpacity;
    [ObservableProperty] private string toolbarOrder = "选择, 画笔, 橡皮, 聚光灯, 板中板, 计时器, 清除, 快捷应用";
    [ObservableProperty] private string quickLaunchApps = string.Empty;

    [ObservableProperty] private string penColor = "#000000";
    [ObservableProperty] private int penWidth = 3;
    [ObservableProperty] private int eraserWidth = 30;
    [ObservableProperty] private int penMode;
    [ObservableProperty] private int highlightWidth = 24;
    [ObservableProperty] private double highlightOpacity = 0.45;
    [ObservableProperty] private string highlightColor = "#FFFF00";
    [ObservableProperty] private int frameRateMode = 1;
    [ObservableProperty] private int penEffect = 1;
    [ObservableProperty] private bool palmErase;

    [ObservableProperty] private bool showStatusBar;
    [ObservableProperty] private bool showStatusTime = true;
    [ObservableProperty] private bool showStatusBattery = true;
    [ObservableProperty] private bool showStatusVolume = true;
    [ObservableProperty] private bool showStatusNetwork = true;
    [ObservableProperty] private bool showStatusSeconds;
    [ObservableProperty] private bool showStatusMusic = true;

    [ObservableProperty] private string overlayScreen = "Auto";
    public int OverlayScreenIndex
    {
        get => OverlayScreen switch { "Primary" => 1, "Screen1" => 2, "Screen2" => 3, "Screen3" => 4, _ => 0 };
        set
        {
            OverlayScreen = value switch { 1 => "Primary", 2 => "Screen1", 3 => "Screen2", 4 => "Screen3", _ => "Auto" };
            OnPropertyChanged();
        }
    }
    [ObservableProperty] private int toolbarPosition = 1;
    [ObservableProperty] private int flipperPosition;
    [ObservableProperty] private int safeArea;
    [ObservableProperty] private double overlayScale = 1;
    [ObservableProperty] private double popWindowScale = 1;
    [ObservableProperty] private bool strictEdgeAlignment;
    [ObservableProperty] private bool toolbarAutoHalfCollapse;
    [ObservableProperty] private bool uiAccessTopmost;
    [ObservableProperty] private bool allowRecording;
    [ObservableProperty] private int zOrderCheckInterval = 200;
    public int ZOrderCheckIntervalIndex
    {
        get => ZOrderCheckInterval switch { 200 => 1, 500 => 2, 1000 => 3, _ => 0 };
        set
        {
            ZOrderCheckInterval = value switch { 1 => 200, 2 => 500, 3 => 1000, _ => 100 };
            OnPropertyChanged();
        }
    }

    [ObservableProperty] private int boardWindowPosition;
    [ObservableProperty] private string boardBackgroundColor = "#FFFFFF";
    [ObservableProperty] private int boardEraserMode = 1;
    [ObservableProperty] private int boardPenEffect = 1;
    [ObservableProperty] private bool boardWindowEnterAnimation = true;
    [ObservableProperty] private int timerFullscreenBehavior;
    [ObservableProperty] private bool timerSoundEffects = true;
    [ObservableProperty] private string timerQuickAddPresets = string.Empty;

    [ObservableProperty] private bool enableNotifications = true;
    [ObservableProperty] private int notificationDuration = 4;
    [ObservableProperty] private int resourceMonitorInterval = 300;
    [ObservableProperty] private bool timerNotifyEnabled = true;

    public int ResourceMonitorIntervalIndex
    {
        get => ResourceMonitorInterval switch
        {
            30 => 1,
            60 => 2,
            300 => 3,
            600 => 4,
            1800 => 5,
            2700 => 6,
            3600 => 7,
            _ => 0,
        };
        set
        {
            ResourceMonitorInterval = value switch
            {
                1 => 30,
                2 => 60,
                3 => 300,
                4 => 600,
                5 => 1800,
                6 => 2700,
                7 => 3600,
                _ => 0,
            };
            OnPropertyChanged();
        }
    }

    [ObservableProperty] private bool randomLinkageEnabled;

    [ObservableProperty] private bool autoCheckUpdates;
    [ObservableProperty] private int updateSource;

    public static MainConfigModel CreateDefault() => new();
}
