using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using Luminalium.Core.Identity;
using System.Diagnostics.CodeAnalysis;

namespace Luminalium.App.ViewModels;

/// <summary>
/// View model for the redesigned "关于" (About) settings page. It aggregates
/// static product identity, environment diagnostics, and update settings.
/// Update checks reuse the injected check delegate (the same path the shell
/// uses) rather than re-implementing download/validation/replacement.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Label/description properties are bound to the UI as instance members so compiled bindings resolve consistently.")]
public sealed partial class AboutSettingsViewModel : ObservableObject
{
    private const string UnavailableText = "—";

    private readonly SettingsCoordinator? _coordinator;
    private readonly DiagnosticService? _diagnosticService;
    private readonly Func<Task<string>>? _checkForUpdatesAsync;
    private readonly Func<string, Task> _copyToClipboard;
    private bool _initializing;

    public AboutSettingsViewModel(
        string? versionDisplay = null,
        string? productName = null,
        string? build = null,
        string? platform = null,
        string? copyright = null,
        string? projectUrl = null,
        string? thirdPartyNotices = null,
        LuminaliumConfig? config = null,
        SettingsCoordinator? coordinator = null,
        DiagnosticService? diagnosticService = null,
        Func<Task<string>>? checkForUpdatesAsync = null,
        Func<string, Task>? copyToClipboard = null)
    {
        _coordinator = coordinator;
        _diagnosticService = diagnosticService;
        _checkForUpdatesAsync = checkForUpdatesAsync;
        _copyToClipboard = copyToClipboard ?? DefaultCopyToClipboardAsync;

        ProductName = string.IsNullOrWhiteSpace(productName)
            ? ProductIdentity.DisplayName
            : productName;
        VersionDisplay = string.IsNullOrWhiteSpace(versionDisplay) ? "未知版本" : versionDisplay;
        Build = string.IsNullOrWhiteSpace(build) ? DeriveBuild(VersionDisplay) : build;
        Platform = string.IsNullOrWhiteSpace(platform) ? DiagnosticService.DefaultPlatform() : platform;
        Copyright = copyright ?? "© 2026 SECTL";
        ProjectUrl = projectUrl ?? "https://github.com/SECTL/Luminalium";
        ThirdPartyNotices = thirdPartyNotices ?? DefaultThirdPartyNotices;

        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync, () => !IsCheckingForUpdates);
        CopyDiagnosticsCommand = new AsyncRelayCommand(CopyDiagnosticsAsync);

        var updates = config?.Updates ?? LuminaliumConfig.CreateDefault().Updates;
        _initializing = true;
        autoCheckEnabled = updates.AutoCheck;
        selectedUpdateSourceIndex = (int)updates.Source;
        if (selectedUpdateSourceIndex < 0 || selectedUpdateSourceIndex >= UpdateSources.Count)
        {
            selectedUpdateSourceIndex = 0;
        }

        _initializing = false;
    }

    public string PageTitle => "关于";

    public string ProductName { get; }

    public string VersionDisplay { get; }

    public string Build { get; }

    public string Platform { get; }

    public string Copyright { get; }

    public string ProjectUrl { get; }

    public string ThirdPartyNotices { get; }

    public IReadOnlyList<string> UpdateSources { get; } = ["GitHub", "镜像"];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool isCheckingForUpdates;

    [ObservableProperty]
    private string updateStatusText = "点击“检查更新”以从更新源获取最新版本。";

    [ObservableProperty]
    private bool autoCheckEnabled;

    [ObservableProperty]
    private int selectedUpdateSourceIndex;

    public IAsyncRelayCommand CheckForUpdatesCommand { get; }

    public IAsyncRelayCommand CopyDiagnosticsCommand { get; }

    // --- About section -------------------------------------------------

    public string AboutSectionTitle => "关于信息";

    public string ProductNameLabel => "产品名";

    public string VersionLabel => "版本";

    public string BuildLabel => "构建";

    public string PlatformLabel => "运行环境";

    public string CopyrightLabel => "版权";

    public string ProjectUrlLabel => "项目链接";

    public string ProjectUrlActionText => "复制链接";

    public string ThirdPartyNoticesLabel => "第三方库致谢";

    // --- Diagnostics section -------------------------------------------

    public string DiagnosticsSectionTitle => "诊断信息";

    public string DiagnosticsDescription => "以下信息用于反馈问题，复制后可直接粘贴给开发者。";

    public string CopyDiagnosticsText => "复制全部诊断信息";

    public string DiagnosticVersion => _diagnosticService?.ApplicationVersion ?? UnavailableText;

    public string DiagnosticPlatform => _diagnosticService?.Platform ?? UnavailableText;

    public string ConfigSchemaVersionText => _diagnosticService?.ConfigSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? UnavailableText;

    public string ConfigDirectoryPath => _diagnosticService?.ConfigDirectoryPath ?? UnavailableText;

    public string ActiveProfileNameText => _diagnosticService?.ActiveProfileName ?? "默认";

    public string LogDirectory => _diagnosticService?.LogDirectory ?? UnavailableText;

    public string UpdateCacheDirectory => _diagnosticService?.UpdateCacheDirectory ?? UnavailableText;

    public string RecentLogCountText => _diagnosticService?.RecentLogCount.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? UnavailableText;

    public string LastErrorLogText => _diagnosticService?.LastErrorLogText ?? "无";

    // --- Update section ------------------------------------------------

    public string UpdateSectionTitle => "更新";

    public string AutoCheckLabel => "启动时自动检查更新";

    public string AutoCheckDescription => "软件启动后自动查询更新源，发现新版本时通过通知提醒你。";

    public string UpdateSourceLabel => "更新源";

    public string CheckForUpdatesText => "检查更新";

    public string UpdateStatusLabel => "更新状态";

    public string AutoCheckStatusText => AutoCheckEnabled ? "开" : "关";

    partial void OnAutoCheckEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(AutoCheckStatusText));
        Persist(update => update.Updates.AutoCheck = value);
    }

    partial void OnSelectedUpdateSourceIndexChanged(int value)
    {
        if (value >= 0 && value < UpdateSources.Count && Enum.IsDefined(typeof(UpdateSource), value))
        {
            Persist(update => update.Updates.Source = (UpdateSource)value);
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        UpdateStatusText = "正在检查更新…";

        try
        {
            if (_checkForUpdatesAsync is null)
            {
                UpdateStatusText = "未配置更新检查。";
                return;
            }

            UpdateStatusText = await _checkForUpdatesAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            UpdateStatusText = $"检查更新失败：{exception.Message}";
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private async Task CopyDiagnosticsAsync()
    {
        if (_diagnosticService is null)
        {
            return;
        }

        await _copyToClipboard(_diagnosticService.BuildDiagnosticText()).ConfigureAwait(true);
    }

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null)
        {
            _ = _coordinator.UpdateAsync(update);
        }
    }

    private static string DeriveBuild(string versionDisplay)
    {
        var separatorIndex = versionDisplay.IndexOf('|');
        if (separatorIndex >= 0 && separatorIndex < versionDisplay.Length - 1)
        {
            var build = versionDisplay[(separatorIndex + 1)..].Trim();
            if (build.Length > 0)
            {
                return build;
            }
        }

        return "未知";
    }

    private static async Task DefaultCopyToClipboardAsync(string text)
    {
        try
        {
            var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
                ? lifetime.MainWindow
                : null;
            var clipboard = topLevel?.Clipboard;
            if (clipboard is not null)
            {
                var transfer = new DataTransfer();
                transfer.Add(DataTransferItem.CreateText(text));
                await clipboard.SetDataAsync(transfer).ConfigureAwait(true);
            }
        }
        catch (Exception)
        {
            // Clipboard access can fail headless or when the window is closing;
            // the copy action is best-effort and never blocks the UI.
        }
    }

    private const string DefaultThirdPartyNotices =
        "荧素万演 使用了以下开源组件：\n" +
        "• Avalonia UI（MIT License）\n" +
        "• CommunityToolkit.Mvvm（MIT License）\n" +
        "• SkiaSharp（MIT License）\n" +
        "• FluentAvalonia（MIT License）\n" +
        "• DotNetCampus.AvaloniaInkCanvas（MIT License）";
}