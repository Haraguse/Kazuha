using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using Luminalium.Attributes;

namespace Luminalium.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    [ObservableProperty] public partial object? FrameContent { get; set; } = null;

    [ObservableProperty] public partial object? DrawerContent { get; set; } = null;
    [ObservableProperty] public partial bool IsDrawerOpen { get; set; } = false;
    [ObservableProperty] public partial bool IsRequestedRestart { get; set; } = false;

    [ObservableProperty] public partial SettingsPageInfo? SelectedPageInfo { get; set; } = null;
    [ObservableProperty] public partial FANavigationViewItemBase? SelectedNavigationViewItem { get; set; } = null;
    public ObservableCollection<FANavigationViewItemBase> NavigationViewItems { get; } = [];
    public ObservableCollection<FANavigationViewItemBase> NavigationViewFooterItems { get; } = [];

}
