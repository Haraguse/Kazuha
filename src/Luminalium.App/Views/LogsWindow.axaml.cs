using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class LogsWindow : FAAppWindow
{
    public LogsWindow()
    {
        InitializeComponent();
        InitializeChrome();
    }

    public LogsWindow(LogsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private void InitializeChrome()
    {
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.Height = 44;
        Icon = new Avalonia.Media.Imaging.Bitmap(AssetLoader.Open(new Uri("avares://Luminalium/Assets/logo.ico")));
    }
}
