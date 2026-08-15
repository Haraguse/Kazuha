using Avalonia.Controls;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class SplashWindow : Window
{
    private bool _hasShown;

    public SplashWindow()
        : this(new SplashViewModel(ShellViewModel.VersionUnavailableText))
    {
    }

    public SplashWindow(SplashViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    public bool HasShown => _hasShown;

    public new void Show()
    {
        _hasShown = true;
        base.Show();
    }

    public new void Close()
    {
        if (_hasShown && IsVisible)
        {
            base.Close();
        }
    }
}
