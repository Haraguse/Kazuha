using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HotAvalonia;
using Luminalium.Extensions.Registry;
using Luminalium.Helpers;
using Luminalium.Services.Logging;
using Luminalium.Services.Config;
using Luminalium.ViewModels;
using Luminalium.Views;
using Luminalium.Views.SettingsPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Luminalium;

public partial class App : Application
{
    public new static App Current => (App)Application.Current!;
    public static bool IsStopping { get; set; } = false;
    private ILogger? _logger;

    public Window? MainWindow { get; private set; }
    public Window? SettingsWindow { get; private set; }
    
    public static bool IsDesktop => Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime;
    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsAcrylicBlurSupported { get; } =
        OperatingSystem.IsWindows()
        && Environment.OSVersion.Version >= new Version(10, 0, 18362, 0)
        && AvaloniaUnsafeAccessorHelpers.GetActiveWin32CompositionMode() ==
        AvaloniaUnsafeAccessorHelpers.Win32CompositionMode.WinUIComposition;
    public static bool IsMicaSupported { get; } =
        OperatingSystem.IsWindows()
        && Environment.OSVersion.Version >= new Version(10, 0, 22000, 0)
        && AvaloniaUnsafeAccessorHelpers.GetActiveWin32CompositionMode() ==
        AvaloniaUnsafeAccessorHelpers.Win32CompositionMode.WinUIComposition;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Resources["NavigationViewItemOnLeftIconBoxHeight"] = 20.0;
        
        if (!Design.IsDesignMode && !OperatingSystem.IsMacOS() && !OperatingSystem.IsAndroid())
        {
            this.UseHotReload();
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BuildHost();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = MainWindow = new MainWindow();
            desktop.MainWindow.Closing += (_, _) => MainWindow = null;
            desktop.Exit += (_, _) => { Stop(); };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void OpenSettingsWindow()
    {
        if (SettingsWindow is { IsVisible: true })
        {
            SettingsWindow.Activate();
            return;
        }

        var settingsWindow = new SettingsWindow();
        settingsWindow.Closed += (_, _) =>
        {
            if (ReferenceEquals(SettingsWindow, settingsWindow))
            {
                SettingsWindow = null;
            }
        };

        SettingsWindow = settingsWindow;
        settingsWindow.Show();
        settingsWindow.Activate();
    }
    
        private void BuildHost()
    {
        IAppHost.Host = Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
#if DEBUG
            .UseEnvironment(Environments.Development)
#else
            .UseEnvironment(Environments.Production)
#endif
            .ConfigureServices(services =>
            {
                // 日志
                services.AddLogging(builder =>
                {
                    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
                    {
                        builder.AddConsoleFormatter<LoggingConsoleFormatter, ConsoleFormatterOptions>();
                        builder.AddConsole(console => { console.FormatterName = "luminalium"; });
                    }

#if DEBUG
                    builder.SetMinimumLevel(LogLevel.Trace);
#endif
                });
                 services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

                 // 配置
                 services.AddSingleton<ConfigServiceBase, DesktopConfigService>();
                 services.AddSingleton<MainConfigHandler>();

                // 服务
                
                // Pages
                 services.AddSettingsPage<GeneralSettingsPage>("常规");
                 services.AddSettingsPage<PersonalizationSettingsPage>("个性化");
                 services.AddSettingsPage<FontSettingsPage>("字体");
                 services.AddSettingsPage<ToolbarSettingsPage>("工具栏");
                 services.AddSettingsPage<LinkageSettingsPage>("联动");
                 services.AddSettingsPage<BuiltInSettingsPage>("内建功能");
                 services.AddSettingsPage<PenSettingsPage>("画笔");
                 services.AddSettingsPage<WindowSettingsPage>("窗口");
                 services.AddSettingsPage<StatusBarSettingsPage>("状态栏");
                 services.AddSettingsPage<StorageSettingsPage>("存储");
                 services.AddSettingsPage<NotificationSettingsPage>("通知");
                 
                 services.AddSettingsPageFooter<MainAboutSettingsPage>("关于");
                 services.AddSettingsPageFooter<HelloSettingsPage>("Test");
                
                // ViewModels
                services.AddTransient<SettingsViewModel>();
            })
            .Build();

        _logger = IAppHost.GetService<ILogger<App>>();
        _logger.LogInformation("Luminalium | Presented by SECTL");
        _logger.LogInformation("Build Host");


        var lifetime = IAppHost.GetService<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Register(Stop);
        lifetime.ApplicationStopped.Register(() => _logger.LogInformation("App Stopped"));

        _ = IAppHost.Host.StartAsync();
        _logger.LogInformation("App Started");
    }

    public void Stop()
    {
        if (IsStopping) return;

        var logger = IAppHost.GetService<ILogger<App>>();
        IsStopping = true;

        Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            logger.LogInformation("App Stopping");

            MainWindow?.Close();

            IAppHost.Host?.StopAsync(TimeSpan.FromSeconds(5));
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        });
    }

    public void Restart()
    {
        Stop();

        var path = Environment.ProcessPath;
        if (path == null) return;

        var executablePath = path.Replace(".dll", GlobalConstants.PlatformExecutableExtension);
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = true
        };
        Process.Start(startInfo);
    }
}
