using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class SettingsWindow : FAAppWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        InitializeChrome();
    }

    public SettingsWindow(SettingsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private void InitializeChrome()
    {
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.Height = 44;

        var assetLoader = AssetLoader.Open(new Uri("avares://Luminalium/Assets/logo.ico"));
        Icon = new Avalonia.Media.Imaging.Bitmap(assetLoader);
    }
}
