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

namespace Luminalium.App;

public partial class App : Application
{
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
            var configurationService = new ConfigurationService(GetSettingsDirectoryPath());
            var configurationLoad = configurationService.Load();
            var localizationService = new LocalizationService();
            var shellViewModel = new ShellViewModel(
                themeService: themeService,
                monetThemeService: MonetThemeServiceFactory.CreateDefault(),
                config: configurationLoad.Config,
                configurationService: configurationService,
                localizationService: localizationService);
            var mainWindow = new MainWindow(shellViewModel, new DialogService());
            var splashWindow = new SplashWindow(new SplashViewModel(shellViewModel.VersionDisplay, localizationService));

            desktop.MainWindow = mainWindow;
            mainWindow.Opened += (_, _) => splashWindow.Close();

            TryShowSplash(splashWindow, shellViewModel);
            TryInstallTrayIcon(desktop, mainWindow, shellViewModel);
        }

        base.OnFrameworkInitializationCompleted();
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

    private static void TryShowSplash(SplashWindow splashWindow, ShellViewModel shellViewModel)
    {
        try
        {
            splashWindow.Show();
        }
        catch (Exception exception)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.Startup, "Shell.Error.SplashUnavailable", exception.Message);
        }
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
