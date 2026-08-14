using Avalonia.Controls;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class AboutDialogContent : UserControl
{
    public AboutDialogContent()
        : this(new AboutDialogViewModel(ShellViewModel.VersionUnavailableText))
    {
    }

    public AboutDialogContent(AboutDialogViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
