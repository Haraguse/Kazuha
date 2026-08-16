using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Luminalium.Attributes;
using Luminalium.Extensions;
using Luminalium.Services;
using Luminalium.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Luminalium.Views;

public partial class SettingsWindow : FAAppWindow, IFANavigationPageFactory
{
    public static SettingsWindow? Current { get; private set; }
    public SettingsViewModel ViewModel { get; } = IAppHost.GetService<SettingsViewModel>();
    private const string DefaultPageId = "settings.hello";
    private bool _isShowingRestartDialog;
    
    public SettingsWindow()
    {
        Current = this;
        DataContext = this;
        InitializeComponent();
        ViewInitializer.InitializeView(this);
        
        TitleBar.Height = 48;
        TitleBar.ExtendsContentIntoTitleBar = true;

        NavigationFrame.NavigationPageFactory = this;
        BuildNavigationMenuItems();
        
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        SelectNavigationItemById(DefaultPageId);
    }

    private void BuildNavigationMenuItems()
    {
        ViewModel.NavigationViewItems.Clear();
        ViewModel.NavigationViewFooterItems.Clear();
        
        ViewModel.NavigationViewItems
            .AddRange(SettingsPagesRegistryService.Items
                .Select(info => info.ToNavigationViewItemBase()));
        
        ViewModel.NavigationViewFooterItems
            .AddRange(SettingsPagesRegistryService.FooterItems
                .Select(info => info.ToNavigationViewItemBase()));
    }
    
    public void SelectNavigationItemById(string id)
    {
        var info = SettingsPagesRegistryService.Items.FirstOrDefault(info => info.Id == id) ??
                   SettingsPagesRegistryService.FooterItems.FirstOrDefault(info => info.Id == id);
        
        if (info != null)
        {
            CoreNavigate(info);
        }
    }
    
    private void SelectNavigationItem(SettingsPageInfo info)
    {
        var item = ViewModel.NavigationViewItems.FirstOrDefault(item => Equals(item.Tag, info)) ??
                   ViewModel.NavigationViewFooterItems.FirstOrDefault(item => Equals(item.Tag, info));
        ViewModel.SelectedNavigationViewItem = item;
    }

    private void CoreNavigate(SettingsPageInfo info)
    {
        CloseDrawer();
        ViewModel.FrameContent = null;
        SelectNavigationItem(info);
        ViewModel.SelectedPageInfo = info;
        NavigationFrame.NavigateFromObject(info);
    }
    
    private void NavigationView_OnItemInvoked(object? sender, FANavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is FANavigationViewItem { Tag: SettingsPageInfo info })
        {
            CoreNavigate(info);
        }
    }

    public Control? GetPage(Type srcType)
    {
        return Activator.CreateInstance(srcType) as Control;
    }

    public Control? GetPageFromObject(object target)
    {
        if (target is not SettingsPageInfo info)
        {
            return null;
        }
        
        return IAppHost.Host!.Services.GetKeyedService<UserControl>(info.Id);
    }
    
    public void OpenDrawer(object content)
    {
        ViewModel.DrawerContent = content;
        ViewModel.IsDrawerOpen = true;
    }

    public void CloseDrawer()
    {
        ViewModel.IsDrawerOpen = false;
    }
    
    public void RequestRestartApp()
    {
        ViewModel.IsRequestedRestart = true;
        _ = ShowRestartDialog();
    }

    private void ButtonRestartApp_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = ShowRestartDialog();
    }

    private async Task ShowRestartDialog()
    {
        if (_isShowingRestartDialog) return;
        _isShowingRestartDialog = true;

        var r = await new FAContentDialog
        {
            Title = "需要重启",
            Content = "部分设置需要重启以应用更改",
            PrimaryButtonText = "重启",
            CloseButtonText = "取消",
            DefaultButton = FAContentDialogButton.Primary
        }.ShowAsync(this);

        _isShowingRestartDialog = false;
        if (r != FAContentDialogResult.Primary)
            return;

        App.Current.Restart();
    }
}