using System.Text.Json.Serialization;
using Luminalium.Core.Localization;

namespace Luminalium.Core.Configuration;

public sealed class LuminaliumConfig
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public AppearanceSettings Appearance { get; set; } = new();
    public GeneralSettings General { get; set; } = new();
    public ToolbarSettings Toolbar { get; set; } = new();
    public LinkageSettings Linkage { get; set; } = new();
    public OverlaySettings Overlay { get; set; } = new();
    [JsonPropertyName("PPT")]
    public PptSettings Ppt { get; set; } = new();
    public SelfPenSettings SelfPen { get; set; } = new();
    public NotificationsSettings Notifications { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public BoardInBoardSettings BoardInBoard { get; set; } = new();
    public TimerSettings Timer { get; set; } = new();
    public FontSettings Fonts { get; set; } = new();
    public UpdateSettings Updates { get; set; } = new();

    public static LuminaliumConfig CreateDefault() => new();
}

public sealed class AppearanceSettings
{
    public ThemeMode ThemeMode { get; set; } = ThemeMode.Light;
    public string ThemeId { get; set; } = "default";
    public string OverlayTheme { get; set; } = "default";
    public string AccentColor { get; set; } = "#3275F5";
    public string FontFamily { get; set; } = string.Empty;
}

public sealed class GeneralSettings
{
    public string Language { get; set; } = AppLanguageExtensions.DefaultCode;
    public bool RunAtStartup { get; set; }
    public bool AutoShowOverlay { get; set; } = true;
    public bool DisableAnimations { get; set; }
    public bool CrashAutoHandleEnabled { get; set; }
    public CrashAutoHandleMode CrashAutoHandleMode { get; set; } = CrashAutoHandleMode.ShowAnalyzer;
    public bool CompatibilityMode { get; set; }
    public bool RegisterUrlProtocol { get; set; }
    public bool UseNativeTitleBar { get; set; }
    public bool HideOnClose { get; set; } = true;
    public SplashMode SplashMode { get; set; } = SplashMode.Always;
    public string SplashStyle { get; set; } = "default";
    public bool ShowDetailedSplash { get; set; }
    public string SplashStartTime { get; set; } = "08:00";
    public string SplashEndTime { get; set; } = "20:00";
    public bool OnboardingCompleted { get; set; }
}

public sealed class ToolbarSettings
{
    public bool ShowClear { get; set; } = true;
    public bool ShowSpotlight { get; set; } = true;
    public bool ShowBoardInBoard { get; set; } = true;
    public bool ShowTimer { get; set; } = true;
    public bool ShowToolbarText { get; set; }
    public bool ShowTooltips { get; set; } = true;
    public string[] QuickLaunchApps { get; set; } = [];
    public string[] ToolbarOrder { get; set; } =
    [
        "select",
        "pen",
        "eraser",
        "spotlight",
        "board_in_board",
        "timer",
        "clear",
        "apps",
    ];
    public string[] DisabledTools { get; set; } = [];
}

public sealed class LinkageSettings
{
    public bool SecRandomEnabled { get; set; }
}

public sealed class OverlaySettings
{
    public bool ShowStatusBar { get; set; }
    public bool StatusBarShowTime { get; set; } = true;
    public bool StatusBarShowSeconds { get; set; }
    public bool StatusBarShowBattery { get; set; } = true;
    public bool StatusBarShowVolume { get; set; } = true;
    public bool StatusBarShowNetwork { get; set; } = true;
    public bool StatusBarShowMusic { get; set; } = true;
    public ClearMode ClearMode { get; set; } = ClearMode.Slide;
    public ToolbarPosition ToolbarPosition { get; set; } = ToolbarPosition.Bottom;
    public FlipperPosition FlipperPosition { get; set; } = FlipperPosition.Center;
    public int SafeArea { get; set; }
    public double Scale { get; set; } = 1.0;
    public double PopWindowScale { get; set; } = 1.0;
    public double ToolbarOpacity { get; set; } = 1.0;
    public double SidePageOpacity { get; set; } = 1.0;
    public bool SyncOpacity { get; set; }
    public bool StrictEdgeAlignment { get; set; }
    public bool ToolbarAutoHalfCollapse { get; set; }
    public bool ToolbarGuideCompleted { get; set; }
    public bool UiAccessTopmost { get; set; }
    public bool AllowRecording { get; set; }
    public int ZOrderCheckInterval { get; set; } = 200;
    public string OverlayScreen { get; set; } = "Auto";
}

public sealed class PptSettings
{
    public bool AutoHandleInk { get; set; } = true;
    public PenMode PenMode { get; set; } = PenMode.Com;
    public int PageTurnRateLimit { get; set; } = 2;
}

public sealed class SelfPenSettings
{
    public int PenWidth { get; set; } = 3;
    public int HighlightWidth { get; set; } = 24;
    public int EraserWidth { get; set; } = 30;
    public double HighlightOpacity { get; set; } = 0.45;
    public string PenColor { get; set; } = "#000000";
    public string HighlightColor { get; set; } = "#FFFF00";
    public PenFrameRateMode FrameRateMode { get; set; } = PenFrameRateMode.Adaptive;
    public PenEffect PenEffect { get; set; } = PenEffect.Limited;
    public bool PalmErase { get; set; }
    public int PenWidthPresetIndex { get; set; } = -1;
    public int EraserWidthPresetIndex { get; set; } = -1;
    public string[] CustomPenColors { get; set; } = [];
    public string[] CustomHighlightColors { get; set; } = [];
}

public sealed class NotificationsSettings
{
    public int ResourceMonitorInterval { get; set; } = 300;
    public bool TimerNotifyEnabled { get; set; } = true;
}

public sealed class SecuritySettings
{
    public bool PasswordProtectionEnabled { get; set; }

    /// <summary>
    /// An algorithm-prefixed salted password hash. The built-in PBKDF2
    /// password service emits <c>pbkdf2$sha256$iterations$salt$derived</c>;
    /// plaintext passwords are not accepted or stored by this configuration model.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
}

public sealed class BoardInBoardSettings
{
    public BoardInBoardPosition WindowPosition { get; set; } = BoardInBoardPosition.BottomRight;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public BoardEraserMode EraserMode { get; set; } = BoardEraserMode.Stroke;
    public BoardPenEffect PenEffect { get; set; } = BoardPenEffect.Limited;
    public bool WindowEnterAnimation { get; set; } = true;
}

public sealed class TimerSettings
{
    public TimerFullscreenBehavior FullscreenBehavior { get; set; } = TimerFullscreenBehavior.Normal;
    public bool EnableSoundEffects { get; set; } = true;
    public int[] QuickAddPresets { get; set; } = [];
}

public sealed class FontSettings
{
    public bool CustomFont { get; set; }
    public string Family { get; set; } = string.Empty;
    public string Weight { get; set; } = "Regular";
    public List<LanguageFont> PerLanguageFonts { get; set; } = [];
    public string PreviewSample { get; set; } = string.Empty;
}

public sealed class LanguageFont
{
    public string LanguageCode { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
}

public sealed class UpdateSettings
{
    public bool AutoCheck { get; set; }
    public UpdateSource Source { get; set; } = UpdateSource.GitHub;
}

public enum UpdateSource
{
    GitHub,
    Mirror,
}

public enum ThemeMode
{
    Light,
    Dark,
    Auto,
}

public enum CrashAutoHandleMode
{
    ShowAnalyzer,
    Exit,
    RestartSilent,
    Toast,
}

public enum SplashMode
{
    Always,
    Never,
    HideOnAutoStart,
    TimeRange,
}

public enum ClearMode
{
    Slide,
    Button,
}

public enum ToolbarPosition
{
    Top,
    Bottom,
    Left,
    Right,
}

public enum FlipperPosition
{
    Center,
    Bottom,
}

public enum PenMode
{
    Com,
    SelfDeveloped,
}

public enum PenFrameRateMode
{
    Low,
    Adaptive,
    High,
}

public enum PenEffect
{
    Off,
    Limited,
    Full,
}

public enum BoardInBoardPosition
{
    BottomRight,
    TopRight,
    BottomLeft,
    TopLeft,
}

public enum BoardEraserMode
{
    Point,
    Stroke,
}

public enum BoardPenEffect
{
    Off,
    Limited,
    Full,
}

public enum TimerFullscreenBehavior
{
    Normal,
    Maximize,
    Fullscreen,
}
