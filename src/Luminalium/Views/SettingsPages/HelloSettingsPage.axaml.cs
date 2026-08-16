using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Luminalium.Attributes;
using Luminalium.Helpers.UI;
using Luminalium.Icons;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.hello", FluentIcons.StarRegular)]
public partial class HelloSettingsPage : UserControl
{
    public HelloSettingsPage()
    {
        InitializeComponent();
    }

    private void ShowFAContentDialog(object? sender, RoutedEventArgs e)
    {
        var dialog = new FAContentDialog
        {
            Title = "Title",
            Content = "Content",
            PrimaryButtonText = "OK",
            SecondaryButtonText = "Cancel",
            DefaultButton = FAContentDialogButton.Primary
        };

        _ = dialog.ShowAsync(TopLevel.GetTopLevel(this));
    }

    private void ShowToast_OnClick(object? sender, RoutedEventArgs e)
    {
        this.ShowSuccessToast("Success!");
    }

    private void OpenDrawer(object? sender, RoutedEventArgs e)
    {
        var content = (Control)this.FindResource("DrawerTest")!;
        content.DataContext = this;
        SettingsWindow.Current?.OpenDrawer(content);
    }

    private void RequestRestart(object? sender, RoutedEventArgs e)
    {
        SettingsWindow.Current?.RequestRestartApp();
    }
}