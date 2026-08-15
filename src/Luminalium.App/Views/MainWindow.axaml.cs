using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using FluentAvalonia.UI.Windowing;
using Luminalium.App.Features;
using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using System.Globalization;

namespace Luminalium.App.Views;

public partial class MainWindow : FAAppWindow
{
    private readonly ShellViewModel _viewModel;
    private readonly IBuiltInFeatureHost _featureHost;
    private readonly ShellNavigationService _navigationService = new();
    private readonly Dictionary<string, FANavigationViewItem> _navigationItems = new(StringComparer.Ordinal);
    private OverlayWindow? _overlayWindow;
    private bool _selectionChanging;

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

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.Height = 44;
        Icon = new Bitmap(AssetLoader.Open(new Uri("avares://Luminalium/Assets/logo.ico")));

        _navigationService.AttachFrame(ShellFrame);
        BuildNavigationItems();
        NavigateFrame(viewModel.CurrentPage);
        SelectNavigationItem(viewModel.CurrentPage.NavigationKey);

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ShellViewModel.CurrentPage))
            {
                NavigateFrame(viewModel.CurrentPage);
                SelectNavigationItem(viewModel.CurrentPage.NavigationKey);
            }
        };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Left && e.KeyModifiers.HasFlag(KeyModifiers.Alt) && _viewModel.CanGoBack)
        {
            _viewModel.GoBack();
            e.Handled = true;
        }

        if (e.Key == Key.O
            && e.KeyModifiers.HasFlag(KeyModifiers.Control)
            && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            OpenOverlay();
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (e.InitialPressMouseButton == MouseButton.XButton1 && _viewModel.CanGoBack)
        {
            _viewModel.GoBack();
            e.Handled = true;
        }
    }

    private void BuildNavigationItems()
    {
        var overviewItem = CreateNavigationItem(
            _viewModel.Overview.Title,
            _viewModel.Overview.NavigationKey,
            _viewModel.Localization["Navigation.Overview.HelpText"]);
        overviewItem.IconSource = new FASymbolIconSource { Symbol = FASymbol.Home };
        AddTopItem(overviewItem);

        foreach (var plugin in _viewModel.BuiltInFeatures)
        {
            AddTopItem(CreateNavigationItem(
                plugin.DisplayName,
                plugin.RouteKey,
                string.Format(CultureInfo.InvariantCulture, _viewModel.Localization["Navigation.Plugin.HelpText"], plugin.DisplayName)));
        }

        var settingsItem = CreateNavigationItem(
            _viewModel.Settings.Title,
            _viewModel.Settings.NavigationKey,
            _viewModel.Localization["Navigation.Settings.HelpText"]);
        ShellNavigation.FooterMenuItems.Add(settingsItem);
        _navigationItems[_viewModel.Settings.NavigationKey] = settingsItem;

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ShellViewModel.LocalizedTextVersion))
            {
                RefreshNavigationText();
            }
        };

        void AddTopItem(FANavigationViewItem item)
        {
            ShellNavigation.MenuItems.Add(item);
            _navigationItems[(string)item.Tag!] = item;
        }
    }

    private void RefreshNavigationText()
    {
        if (_navigationItems.TryGetValue(_viewModel.Overview.NavigationKey, out var overviewItem))
        {
            SetNavigationText(overviewItem, _viewModel.Overview.Title, _viewModel.Localization["Navigation.Overview.HelpText"]);
        }

        if (_navigationItems.TryGetValue(_viewModel.Settings.NavigationKey, out var settingsItem))
        {
            SetNavigationText(settingsItem, _viewModel.Settings.Title, _viewModel.Localization["Navigation.Settings.HelpText"]);
        }

        foreach (var plugin in _viewModel.BuiltInFeatures)
        {
            var key = plugin.RouteKey;
            if (_navigationItems.TryGetValue(key, out var pluginItem))
            {
                SetNavigationText(pluginItem, plugin.DisplayName, string.Format(CultureInfo.InvariantCulture, _viewModel.Localization["Navigation.Plugin.HelpText"], plugin.DisplayName));
            }
        }
    }

    private static void SetNavigationText(FANavigationViewItem item, string content, string helpText)
    {
        item.Content = content;
        Avalonia.Automation.AutomationProperties.SetName(item, content);
        Avalonia.Automation.AutomationProperties.SetHelpText(item, helpText);
    }

    private FANavigationViewItem CreateNavigationItem(string content, string tag, string helpText)
    {
        var item = new FANavigationViewItem
        {
            Content = content,
            Tag = tag,
            MinHeight = 40,
        };
        item.Tapped += OnNavigationItemTapped;
        Avalonia.Automation.AutomationProperties.SetName(item, content);
        Avalonia.Automation.AutomationProperties.SetHelpText(item, helpText);
        return item;
    }

    private void OnNavigationItemTapped(object? sender, TappedEventArgs e)
    {
        if (_selectionChanging || sender is not FANavigationViewItem { Tag: string tag })
        {
            return;
        }

        NavigateByTag(tag);
        e.Handled = true;
    }

    private void OnItemInvoked(object? sender, FANavigationViewItemInvokedEventArgs e)
    {
        if (_selectionChanging || e.InvokedItemContainer?.Tag is not string tag)
        {
            return;
        }

        NavigateByTag(tag);
    }

    private void OnSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        var container = e.SelectedItemContainer ?? e.SelectedItem as FANavigationViewItem;
        if (_selectionChanging || container?.Tag is not string tag)
        {
            return;
        }

        NavigateByTag(tag);
    }

    private void NavigateByTag(string tag)
    {
        if (tag == _viewModel.Overview.NavigationKey)
        {
            _viewModel.NavigateToOverview();
            return;
        }

        if (tag == _viewModel.Settings.NavigationKey)
        {
            _viewModel.NavigateToSettings();
            return;
        }

        _ = ActivateFeatureAsync(tag);
    }

    private async Task ActivateFeatureAsync(string route)
    {
        var parsed = new BuiltInFeatureRouteParser().Parse(route);
        if (!parsed.IsSuccess) return;
        if (parsed.FeatureId == BuiltInFeatureId.Settings || parsed.FeatureId == BuiltInFeatureId.Onboarding || parsed.FeatureId == BuiltInFeatureId.Logs)
        {
            _viewModel.NavigateToFeature(route);
            return;
        }
        await _featureHost.ActivateAsync(parsed.FeatureId!.Value, route, parsed.CorrelationId).ConfigureAwait(true);
    }

    private BuiltInFeatureHost CreateDefaultFeatureHost() => NativeBuiltInFeatureHostFactory.Create(() => this);

    private void OnBackRequested(object? sender, FANavigationViewBackRequestedEventArgs e) => _viewModel.GoBack();

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

    public void OpenOverlay()
    {
        if (_overlayWindow is { IsVisible: true })
        {
            _overlayWindow.Activate();
            return;
        }

        var overlayWindow = new OverlayWindow(_viewModel.Localization);
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
        overlayWindow.Show(this);
        overlayWindow.Activate();
    }

    private void NavigateFrame(ShellPageViewModel page)
    {
        _navigationService.Navigate(page);
    }

    private void SelectNavigationItem(string navigationKey)
    {
        if (!_navigationItems.TryGetValue(navigationKey, out var item))
        {
            return;
        }

        _selectionChanging = true;
        ShellNavigation.SelectedItem = item;
        _selectionChanging = false;
    }
}
