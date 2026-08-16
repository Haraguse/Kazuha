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
using System.Globalization;

namespace Luminalium.App;

public partial class App : Application
{
    private static readonly SplashPolicyService SplashPolicy = new();
    private TrayIcons? _trayIcons;

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
            var splashPolicy = EvaluateSplashPolicy(configurationLoad.Config, isAutoStart: false);
            var splashLifecycle = new StartupSplashLifecycle<SplashWindow>(
                () => new SplashWindow(new SplashViewModel(shellViewModel.VersionDisplay, localizationService, splashPolicy)),
                splash => splash.Show(),
                splash => splash.Close(),
                () => desktop.Shutdown());

            desktop.MainWindow = mainWindow;
            Exception? splashException = null;
            mainWindow.Opened += async (_, _) =>
            {
                splashLifecycle.Dismiss();
                if (splashException is not null)
                {
                    await startupErrors.PresentAsync(
                        shellViewModel,
                        splashException,
                        async () =>
                        {
                            return await splashLifecycle.RetryAsync(
                                () => Task.FromResult(true)).ConfigureAwait(true);
                        },
                        () => desktop.Shutdown()).ConfigureAwait(true);
                }
            };

            if (splashPolicy.IsVisible)
            {
                splashException = splashLifecycle.TryShowFresh()
                    ? null
                    : CreateSplashException(shellViewModel);
            }
            TryInstallTrayIcon(desktop, mainWindow, shellViewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static SplashPolicyResult EvaluateSplashPolicy(LuminaliumConfig config, bool isAutoStart)
    {
        ArgumentNullException.ThrowIfNull(config);
        // Startup origin is explicit. The current app has no reliable launch-origin signal, so App passes false.
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
        openSettings.Click += (_, _) =>
        {
            ShowMainWindow(mainWindow);
            shellViewModel.NavigateToSettings();
        };
        menu.Items.Add(openSettings);

        var openOverlay = new NativeMenuItem(localization["Tray.Menu.OpenOverlay"]);
        openOverlay.Click += (_, _) =>
        {
            ShowMainWindow(mainWindow);
            mainWindow.OpenOverlay();
        };
        menu.Items.Add(openOverlay);

        menu.Items.Add(new NativeMenuItemSeparator());

        var exit = new NativeMenuItem(localization["Tray.Menu.Exit"]);
        exit.Click += (_, _) => desktop.Shutdown();
        menu.Items.Add(exit);

        localization.LanguageChanged += (_, _) =>
        {
            showWindow.Header = localization["Tray.Menu.ShowWindow"];
            openSettings.Header = localization["Tray.Menu.OpenSettings"];
            openOverlay.Header = localization["Tray.Menu.OpenOverlay"];
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
