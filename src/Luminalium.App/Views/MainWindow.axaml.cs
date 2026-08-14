using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using FluentAvalonia.UI.Windowing;
using Luminalium.App.Services;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class MainWindow : FAAppWindow
{
    private readonly ShellViewModel _viewModel;
    private readonly ShellNavigationService _navigationService = new();
    private readonly Dictionary<string, FANavigationViewItem> _navigationItems = new(StringComparer.Ordinal);
    private bool _selectionChanging;

    public MainWindow()
        : this(new ShellViewModel(), new DialogService())
    {
    }

    public MainWindow(ShellViewModel viewModel, DialogService dialogService)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        dialogService.AttachOwner(this);
        viewModel.DialogService = dialogService;

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
        var overviewItem = CreateNavigationItem("Overview", _viewModel.Overview.NavigationKey, "Open Luminalium overview");
        overviewItem.IconSource = new FASymbolIconSource { Symbol = FASymbol.Home };
        AddTopItem(overviewItem);

        foreach (var plugin in _viewModel.Plugins)
        {
            AddTopItem(CreateNavigationItem(plugin.DisplayName, $"plugin:{plugin.Id}", $"Open {plugin.DisplayName}"));
        }

        var settingsItem = CreateNavigationItem("Settings", _viewModel.Settings.NavigationKey, "Open Luminalium settings");
        ShellNavigation.FooterMenuItems.Add(settingsItem);
        _navigationItems[_viewModel.Settings.NavigationKey] = settingsItem;

        void AddTopItem(FANavigationViewItem item)
        {
            ShellNavigation.MenuItems.Add(item);
            _navigationItems[(string)item.Tag!] = item;
        }
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

        const string pluginPrefix = "plugin:";
        if (tag.StartsWith(pluginPrefix, StringComparison.Ordinal))
        {
            var plugin = _viewModel.Plugins.FirstOrDefault(candidate =>
                StringComparer.Ordinal.Equals(candidate.Id, tag[pluginPrefix.Length..]));
            _viewModel.NavigateToPlugin(plugin);
        }
    }

    private void OnBackRequested(object? sender, FANavigationViewBackRequestedEventArgs e) => _viewModel.GoBack();

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
