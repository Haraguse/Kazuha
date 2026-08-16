using Avalonia.Interactivity;
using FluentAvalonia.UI.Windowing;
using Luminalium.Extensions;

namespace Luminalium.Views;

public partial class MainWindow : FAAppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        ViewInitializer.InitializeView(this);
    }

    private void OpenSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        App.Current.OpenSettingsWindow();
    }
}