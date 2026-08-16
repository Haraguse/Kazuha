using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Luminalium.App.ViewModels;

/// <summary>
/// Placeholder view model for the redesigned "常规" (General) settings page.
/// Currently exposes only UI-bound properties; persistence and real behavior
/// will be wired up in a follow-up task.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Label/description properties are bound to the UI as instance members so design-time and runtime bindings resolve consistently.")]
public sealed partial class GeneralSettingsViewModel : ObservableObject
{
    private readonly SettingsCoordinator? _coordinator;
    private bool _initializing;

    public GeneralSettingsViewModel(LuminaliumConfig? config = null, SettingsCoordinator? coordinator = null)
    {
        _coordinator = coordinator;
        var general = config?.General ?? LuminaliumConfig.CreateDefault().General;
        _initializing = true;
        selectedLanguageIndex = Array.IndexOf(LanguageCodes.ToArray(), general.Language);
        if (selectedLanguageIndex < 0) selectedLanguageIndex = 0;
        autoStartEnabled = general.RunAtStartup;
        autoShowOverlayEnabled = general.AutoShowOverlay;
        urlProtocolEnabled = general.RegisterUrlProtocol;
        disableAnimations = general.DisableAnimations;
        crashHandlerEnabled = general.CrashAutoHandleEnabled;
        selectedCrashHandlerModeIndex = (int)general.CrashAutoHandleMode;
        pageRateLimit = general.PageTurnRateLimitFallback(config);
        compatibilityModeEnabled = general.CompatibilityMode;
        _initializing = false;
    }

    private static readonly IReadOnlyList<string> LanguageCodes = ["zh-CN", "zh-TW", "ja-JP", "en-US", "ug-CN", "yue-HK"];

    [ObservableProperty]
    private int selectedLanguageIndex;

    [ObservableProperty]
    private bool autoStartEnabled;

    [ObservableProperty]
    private bool urlProtocolEnabled;

    [ObservableProperty]
    private bool autoShowOverlayEnabled;

    [ObservableProperty]
    private bool disableAnimations;

    [ObservableProperty]
    private bool crashHandlerEnabled;

    [ObservableProperty]
    private int selectedCrashHandlerModeIndex;

    [ObservableProperty]
    private double pageRateLimit = 2;

    [ObservableProperty]
    private bool passwordProtectionEnabled;

    [ObservableProperty]
    private bool compatibilityModeEnabled;

    public IReadOnlyList<string> Languages { get; } =
    [
        "简体中文",
        "繁體中文",
        "日本語",
        "English",
        "维吾尔语",
        "粤语"
    ];

    public IReadOnlyList<string> CrashHandlerModes { get; } =
    [
        "打开崩溃详情",
        "直接退出",
        "静默重启",
        "发送通知"
    ];

    public string PageTitle => "常规";

    public string LanguageSectionTitle => "语言";

    public string InterfaceLanguageLabel => "界面语言";

    public string InterfaceLanguageDescription => "更改语言后需重启软件才能在所有界面生效";

    public string DefaultBehaviorSectionTitle => "默认行为";

    public string AutoStartLabel => "开机自启";

    public string AutoStartDescription => "在 Windows 登录后自动启动 荧素万演，仅对当前用户生效";

    public string UrlProtocolLabel => "注册 URL 协议";

    public string UrlProtocolDescription => "允许第三方应用通过 luminalium:// 链接打开设置、计时器和小黑板等功能";

    public string DisableAnimationsLabel => "禁用界面动画";

    public string DisableAnimationsDescription => "关闭全局过渡、缩放与滚动动画；开启后各界面会直接切换到最终状态";

    public string CrashHandlerLabel => "异常退出处理";

    public string CrashHandlerDescription => "程序未捕获异常时，自动打开崩溃详情、后台重启、发送通知或直接退出";

    public string PageRateLimitLabel => "翻页频率上限";

    public string PageRateLimitDescription => "限制每秒最多受理的翻页请求次数，超出的点击会被忽略";

    public string SecuritySectionTitle => "安全";

    public string PasswordProtectionLabel => "设置密码保护";

    public string PasswordProtectionDescription => "启用后打开设置需要输入密码";

    public string CompatibilitySectionTitle => "兼容性";

    public string CompatibilityModeLabel => "兼容模式";

    public string CompatibilityModeDescription => "让悬浮窗常驻显示，翻页改用模拟键盘输入，并关闭自动显隐、页码和部分工具";

    public string AutoStartStatusText => AutoStartEnabled ? "开" : "关";

    public string AutoShowOverlayLabel => "自动显示 Overlay";

    public string AutoShowOverlayDescription => "检测到幻灯片放映时自动打开 Overlay";

    public string AutoShowOverlayStatusText => AutoShowOverlayEnabled ? "开" : "关";

    public string UrlProtocolStatusText => UrlProtocolEnabled ? "开" : "关";

    public string DisableAnimationsStatusText => DisableAnimations ? "开" : "关";

    public string CrashHandlerStatusText => CrashHandlerEnabled ? "开" : "关";

    public string PasswordProtectionStatusText => PasswordProtectionEnabled ? "开" : "关";

    public string CompatibilityModeStatusText => CompatibilityModeEnabled ? "开" : "关";

    public string PageRateLimitDisplay => PageRateLimit.ToString("0", System.Globalization.CultureInfo.InvariantCulture);

    partial void OnAutoStartEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(AutoStartStatusText));
        Persist(c => c.General.RunAtStartup = value);
    }

    partial void OnAutoShowOverlayEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(AutoShowOverlayStatusText));
        Persist(c => c.General.AutoShowOverlay = value);
    }

    partial void OnUrlProtocolEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(UrlProtocolStatusText));
        Persist(c => c.General.RegisterUrlProtocol = value);
    }

    partial void OnDisableAnimationsChanged(bool value)
    {
        OnPropertyChanged(nameof(DisableAnimationsStatusText));
        Persist(c => c.General.DisableAnimations = value);
    }

    partial void OnCrashHandlerEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(CrashHandlerStatusText));
        Persist(c => c.General.CrashAutoHandleEnabled = value);
    }

    partial void OnPasswordProtectionEnabledChanged(bool value) => OnPropertyChanged(nameof(PasswordProtectionStatusText));

    partial void OnCompatibilityModeEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(CompatibilityModeStatusText));
        Persist(c => c.General.CompatibilityMode = value);
    }

    partial void OnPageRateLimitChanged(double value)
    {
        OnPropertyChanged(nameof(PageRateLimitDisplay));
        Persist(c => c.Ppt.PageTurnRateLimit = (int)Math.Round(value));
    }

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        if (value >= 0 && value < LanguageCodes.Count) Persist(c => c.General.Language = LanguageCodes[value]);
    }

    partial void OnSelectedCrashHandlerModeIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(CrashAutoHandleMode), value)) Persist(c => c.General.CrashAutoHandleMode = (CrashAutoHandleMode)value);
    }

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null)
        {
            _ = _coordinator.UpdateAsync(update);
        }
    }
}

file static class GeneralSettingsExtensions
{
    public static double PageTurnRateLimitFallback(this GeneralSettings general, LuminaliumConfig? config) =>
        config?.Ppt.PageTurnRateLimit ?? 2;
}
