using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;
using Luminalium.App.Features;
using Luminalium.App.Services;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class MainWindow : FAAppWindow
{
    private readonly ShellViewModel _viewModel;
    private readonly IBuiltInFeatureHost _featureHost;
    private OverlayWindow? _overlayWindow;
    private SettingsWindow? _settingsWindow;
    private LogsWindow? _logsWindow;
    private OnboardingWindow? _onboardingWindow;

    public MainWindow()
        : this(new ShellViewModel(), new DialogService())
    {
    }

    public MainWindow(ShellViewModel viewModel, DialogService dialogService, IBuiltInFeatureHost? featureHost = null)
    {
        _viewModel = viewModel;
        _featureHost = featureHost ?? CreateDefaultFeatureHost();
        DataContext = viewModel;
        InitializeComponent();

        dialogService.AttachOwner(this);
        viewModel.DialogService = dialogService;
        viewModel.RestartRequired += (_, _) => ShutdownForRestart();
        viewModel.RequestOpenSettingsWindow += (_, _) => OpenSettingsWindow();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.O
            && e.KeyModifiers.HasFlag(KeyModifiers.Control)
            && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            OpenOverlay();
            e.Handled = true;
        }
    }

    private static BuiltInFeatureHost CreateDefaultFeatureHost() => NativeBuiltInFeatureHostFactory.Create();

    protected override async void OnClosed(EventArgs e)
    {
        await _featureHost.DisposeAsync().ConfigureAwait(true);
        base.OnClosed(e);
    }

    private void ShutdownForRestart()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
            return;
        }

        Close();
    }

    private void OnOpenOverlayClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => OpenOverlay();

    private void OnOpenLogsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => OpenLogsWindow();

    public void OpenOverlay()
    {
        if (_overlayWindow is { IsVisible: true })
        {
            _overlayWindow.Activate();
            return;
        }

        var overlayWindow = new OverlayWindow(_viewModel.Localization, _viewModel.SettingsCoordinator);
        overlayWindow.Closed += (_, _) =>
        {
            if (ReferenceEquals(_overlayWindow, overlayWindow))
            {
                _overlayWindow = null;
            }

            if (IsVisible)
            {
                Activate();
            }
        };

        _overlayWindow = overlayWindow;
        overlayWindow.Show();
        overlayWindow.Activate();
    }

    public void OpenSettingsWindow()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        var settingsWindow = new SettingsWindow(_viewModel.Settings);
        settingsWindow.Closed += (_, _) =>
        {
            if (ReferenceEquals(_settingsWindow, settingsWindow))
            {
                _settingsWindow = null;
            }
        };

        _settingsWindow = settingsWindow;
        settingsWindow.Show();
        settingsWindow.Activate();
    }

    public void OpenLogsWindow()
    {
        if (_logsWindow is { IsVisible: true })
        {
            _logsWindow.Activate();
            return;
        }

        var logsWindow = new LogsWindow(_viewModel.Logs);
        logsWindow.Closed += (_, _) =>
        {
            if (ReferenceEquals(_logsWindow, logsWindow))
            {
                _logsWindow = null;
            }
        };

        _logsWindow = logsWindow;
        logsWindow.Show();
        logsWindow.Activate();
    }

    public void OpenOnboardingWindow()
    {
        if (_onboardingWindow is { IsVisible: true })
        {
            _onboardingWindow.Activate();
            return;
        }

        var onboardingWindow = new OnboardingWindow(_viewModel.Onboarding);
        onboardingWindow.Closed += (_, _) =>
        {
            if (ReferenceEquals(_onboardingWindow, onboardingWindow))
            {
                _onboardingWindow = null;
            }
        };

        _onboardingWindow = onboardingWindow;
        onboardingWindow.Show();
        onboardingWindow.Activate();
    }

    public async Task ActivateFeatureAsync(string route)
    {
        var parsed = new BuiltInFeatureRouteParser().Parse(route);
        if (!parsed.IsSuccess)
        {
            return;
        }

        if (parsed.FeatureId == BuiltInFeatureId.Settings)
        {
            OpenSettingsWindow();
            return;
        }

        if (parsed.FeatureId == BuiltInFeatureId.Onboarding)
        {
            OpenOnboardingWindow();
            return;
        }

        if (parsed.FeatureId == BuiltInFeatureId.Logs)
        {
            OpenLogsWindow();
            return;
        }

        await _featureHost.ActivateAsync(parsed.FeatureId!.Value, route, parsed.CorrelationId).ConfigureAwait(true);
    }
}
