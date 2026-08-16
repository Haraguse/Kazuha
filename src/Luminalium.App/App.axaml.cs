using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Luminalium.App.Views;
using Luminalium.Core.Configuration;
using Luminalium.Core.Identity;
using Luminalium.Core.Localization;
using Luminalium.Theming;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Luminalium.App;

[SuppressMessage(
    "Design",
    "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
    Justification = "The App root lives for the whole process; _slideshowWatcher is disposed on desktop.Exit.")]
public partial class App : Application
{
    private static readonly SplashPolicyService SplashPolicy = new();
    private TrayIcons? _trayIcons;
    private SlideshowWatcher? _slideshowWatcher;
    private MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var themeService = new AvaloniaShellThemeService(this);
            var settingsDirectory = GetSettingsDirectoryPath();
            var configurationService = new ConfigurationService(settingsDirectory);
            var configurationLoad = configurationService.Load();
            var localizationService = new LocalizationService();
            var logService = new LocalLogService(logPath: Path.Combine(settingsDirectory, "logs", "luminalium.log"));
            var versionPath = Path.Combine(AppContext.BaseDirectory, ProductIdentity.VersionMetadataFileName);
            var versionResult = VersionMetadataReader.Load(versionPath);
            var versionText = versionResult is { IsSuccess: true, Metadata: not null }
                ? versionResult.Metadata.Version
                : "unknown";
            logService.Log(LogSeverity.Information, $"Luminalium started (version {versionText}).", "App");
            logService.Log(LogSeverity.Information, $"Settings directory: {settingsDirectory}.", "App");
            var shellViewModel = new ShellViewModel(
                themeService: themeService,
                monetThemeService: MonetThemeServiceFactory.CreateDefault(),
                config: configurationLoad.Config,
                configurationService: configurationService,
                localizationService: localizationService,
                logService: logService);
            var dialogService = new DialogService();
            var startupErrors = new StartupErrorCoordinator(dialogService);
            var mainWindow = new MainWindow(shellViewModel, dialogService);
            _mainWindow = mainWindow;
            var splashPolicy = EvaluateSplashPolicy(configurationLoad.Config, isAutoStart: false);
            var splashLifecycle = new StartupSplashLifecycle<SplashWindow>(
                () => new SplashWindow(new SplashViewModel(shellViewModel.VersionDisplay, localizationService, splashPolicy)),
                splash => splash.Show(),
                splash => splash.Close(),
                () => desktop.Shutdown());

            // The main window is a hidden owner: it is never shown at startup.
            // The app stays resident in the tray and opens standalone windows
            // (settings, logs, onboarding, features) on demand.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Exception? splashException = null;

            // Show splash if configured
            if (splashPolicy.IsVisible)
            {
                splashException = splashLifecycle.TryShowFresh()
                    ? null
                    : CreateSplashException(shellViewModel);
            }

            // Dismiss splash after a short delay or on window open
            var splashDismissTimer = new System.Timers.Timer(1200) { AutoReset = false };
            splashDismissTimer.Elapsed += (_, _) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => splashLifecycle.Dismiss());
            };
            splashDismissTimer.Start();

            // Handle startup errors
            if (splashException is not null)
            {
                _ = startupErrors.PresentAsync(
                    shellViewModel,
                    splashException,
                    async () => true,
                    () => desktop.Shutdown());
            }

            // Show onboarding on first run
            if (!configurationLoad.Config.General.OnboardingCompleted)
            {
                mainWindow.OpenOnboardingWindow();
            }

            // Install tray icon
            TryInstallTrayIcon(desktop, mainWindow, shellViewModel);
            TryStartSlideshowWatcher(mainWindow, configurationLoad.Config);
            desktop.Exit += (_, _) =>
            {
                _slideshowWatcher?.Dispose();
                _slideshowWatcher = null;
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static SplashPolicyResult EvaluateSplashPolicy(LuminaliumConfig config, bool isAutoStart)
    {
        ArgumentNullException.ThrowIfNull(config);
        return SplashPolicy.Evaluate(config.General, isAutoStart, TimeOnly.FromDateTime(DateTime.Now));
    }

    private static InvalidOperationException CreateSplashException(ShellViewModel shellViewModel)
    {
        shellViewModel.ReportLocalizedError(ShellErrorKind.Startup, "Shell.Error.SplashUnavailable");
        return new InvalidOperationException("Splash window could not be shown.");
    }

    private static string GetSettingsDirectoryPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = Path.GetTempPath();
        }

        return Path.Combine(localAppData, ProductIdentity.DisplayName);
    }

    private void TryInstallTrayIcon(
        IClassicDesktopStyleApplicationLifetime desktop,
        MainWindow mainWindow,
        ShellViewModel shellViewModel)
    {
        try
        {
            var trayIcon = new TrayIcon
            {
                Icon = LoadWindowIcon(),
                ToolTipText = ProductIdentity.DisplayName,
                Menu = BuildTrayMenu(desktop, mainWindow, shellViewModel),
                IsVisible = true,
            };

            trayIcon.Clicked += (_, _) => ShowMainWindow(mainWindow);
            _trayIcons = new TrayIcons { trayIcon };
            TrayIcon.SetIcons(this, _trayIcons);
            shellViewModel.SetTrayStatus("Tray.Status.Active");
        }
        catch (Exception exception)
        {
            shellViewModel.SetTrayStatus("Tray.Status.Unavailable");
            shellViewModel.ReportLocalizedError(ShellErrorKind.Startup, "Shell.Error.TrayUnavailable", exception.Message);
        }
    }

    private void TryStartSlideshowWatcher(MainWindow mainWindow, LuminaliumConfig config)
    {
        if (!config.General.AutoShowOverlay)
        {
            return;
        }

        var windowAdapter = PresentationHostFactory.TryCreateWindowAdapter();
        if (windowAdapter is null)
        {
            return;
        }

        _slideshowWatcher = new SlideshowWatcher(windowAdapter, mainWindow.OpenOverlay);
        _slideshowWatcher.Start();
    }

    private static NativeMenu BuildTrayMenu(
        IClassicDesktopStyleApplicationLifetime desktop,
        MainWindow mainWindow,
        ShellViewModel shellViewModel)
    {
        var menu = new NativeMenu();
        var localization = shellViewModel.Localization;

        var showWindow = new NativeMenuItem(localization["Tray.Menu.ShowWindow"]);
        showWindow.Click += (_, _) => ShowMainWindow(mainWindow);
        menu.Items.Add(showWindow);

        var openSettings = new NativeMenuItem(localization["Tray.Menu.OpenSettings"]);
        openSettings.Click += (_, _) => mainWindow.OpenSettingsWindow();
        menu.Items.Add(openSettings);

        var openOverlay = new NativeMenuItem(localization["Tray.Menu.OpenOverlay"]);
        openOverlay.Click += (_, _) => mainWindow.OpenOverlay();
        menu.Items.Add(openOverlay);

        var openLogs = new NativeMenuItem(localization["Tray.Menu.OpenLogs"]);
        openLogs.Click += (_, _) => mainWindow.OpenLogsWindow();
        menu.Items.Add(openLogs);

        menu.Items.Add(new NativeMenuItemSeparator());

        // Feature windows
        var openBoard = new NativeMenuItem(localization["Tray.Menu.OpenBoard"]);
        openBoard.Click += (_, _) => _ = mainWindow.ActivateFeatureAsync("board");
        menu.Items.Add(openBoard);

        var openTimer = new NativeMenuItem(localization["Tray.Menu.OpenTimer"]);
        openTimer.Click += (_, _) => _ = mainWindow.ActivateFeatureAsync("timer");
        menu.Items.Add(openTimer);

        var openSpotlight = new NativeMenuItem(localization["Tray.Menu.OpenSpotlight"]);
        openSpotlight.Click += (_, _) => _ = mainWindow.ActivateFeatureAsync("spotlight");
        menu.Items.Add(openSpotlight);

        var openAppLauncher = new NativeMenuItem(localization["Tray.Menu.OpenAppLauncher"]);
        openAppLauncher.Click += (_, _) => _ = mainWindow.ActivateFeatureAsync("app_launcher");
        menu.Items.Add(openAppLauncher);

        var openStatusBar = new NativeMenuItem(localization["Tray.Menu.OpenStatusBar"]);
        openStatusBar.Click += (_, _) => _ = mainWindow.ActivateFeatureAsync("status_bar");
        menu.Items.Add(openStatusBar);

        menu.Items.Add(new NativeMenuItemSeparator());

        // Directory items
        var openProgramDir = new NativeMenuItem(localization["Tray.Menu.OpenProgramDirectory"]);
        openProgramDir.Click += (_, _) => _ = Process.Start(new ProcessStartInfo(AppContext.BaseDirectory) { UseShellExecute = true });
        menu.Items.Add(openProgramDir);

        var openUserDir = new NativeMenuItem(localization["Tray.Menu.OpenUserDirectory"]);
        openUserDir.Click += (_, _) => _ = Process.Start(new ProcessStartInfo(GetSettingsDirectoryPath()) { UseShellExecute = true });
        menu.Items.Add(openUserDir);

        menu.Items.Add(new NativeMenuItemSeparator());

        var exit = new NativeMenuItem(localization["Tray.Menu.Exit"]);
        exit.Click += (_, _) => desktop.Shutdown();
        menu.Items.Add(exit);

        localization.LanguageChanged += (_, _) =>
        {
            showWindow.Header = localization["Tray.Menu.ShowWindow"];
            openSettings.Header = localization["Tray.Menu.OpenSettings"];
            openOverlay.Header = localization["Tray.Menu.OpenOverlay"];
            openLogs.Header = localization["Tray.Menu.OpenLogs"];
            openBoard.Header = localization["Tray.Menu.OpenBoard"];
            openTimer.Header = localization["Tray.Menu.OpenTimer"];
            openSpotlight.Header = localization["Tray.Menu.OpenSpotlight"];
            openAppLauncher.Header = localization["Tray.Menu.OpenAppLauncher"];
            openStatusBar.Header = localization["Tray.Menu.OpenStatusBar"];
            openProgramDir.Header = localization["Tray.Menu.OpenProgramDirectory"];
            openUserDir.Header = localization["Tray.Menu.OpenUserDirectory"];
            exit.Header = localization["Tray.Menu.Exit"];
        };

        return menu;
    }

    private static void ShowMainWindow(Window mainWindow)
    {
        if (!mainWindow.IsVisible)
        {
            mainWindow.Show();
        }

        mainWindow.Activate();
    }

    private static WindowIcon LoadWindowIcon()
    {
        var stream = AssetLoader.Open(new Uri("avares://Luminalium/Assets/logo.ico"));
        return new WindowIcon(stream);
    }
}