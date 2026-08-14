using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Luminalium.App.Views;
using Luminalium.Core.Identity;

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
            var shellViewModel = new ShellViewModel(themeService: themeService);
            var mainWindow = new MainWindow(shellViewModel, new DialogService());
            var splashWindow = new SplashWindow(new SplashViewModel(shellViewModel.VersionDisplay));

            desktop.MainWindow = mainWindow;
            mainWindow.Opened += (_, _) => splashWindow.Close();

            TryShowSplash(splashWindow, shellViewModel);
            TryInstallTrayIcon(desktop, mainWindow, shellViewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void TryShowSplash(SplashWindow splashWindow, ShellViewModel shellViewModel)
    {
        try
        {
            splashWindow.Show();
        }
        catch (Exception exception)
        {
            shellViewModel.ReportError(ShellErrorKind.Startup, $"Splash unavailable: {exception.Message}");
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
            shellViewModel.TrayStatusText = "Tray icon active";
        }
        catch (Exception exception)
        {
            shellViewModel.TrayStatusText = "Tray icon unavailable";
            shellViewModel.ReportError(ShellErrorKind.Startup, $"Tray unavailable: {exception.Message}");
        }
    }

    private static NativeMenu BuildTrayMenu(
        IClassicDesktopStyleApplicationLifetime desktop,
        MainWindow mainWindow,
        ShellViewModel shellViewModel)
    {
        var menu = new NativeMenu();

        var showWindow = new NativeMenuItem("Show window");
        showWindow.Click += (_, _) => ShowMainWindow(mainWindow);
        menu.Items.Add(showWindow);

        var openSettings = new NativeMenuItem("Open settings");
        openSettings.Click += (_, _) =>
        {
            ShowMainWindow(mainWindow);
            shellViewModel.NavigateToSettings();
        };
        menu.Items.Add(openSettings);

        menu.Items.Add(new NativeMenuItemSeparator());

        var exit = new NativeMenuItem("Exit");
        exit.Click += (_, _) => desktop.Shutdown();
        menu.Items.Add(exit);

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
