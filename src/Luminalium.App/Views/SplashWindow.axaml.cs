using Avalonia.Controls;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
        : this(new SplashViewModel(ShellViewModel.VersionUnavailableText))
    {
    }

    public SplashWindow(SplashViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
