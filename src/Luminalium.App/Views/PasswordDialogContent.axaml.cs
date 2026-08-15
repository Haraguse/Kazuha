using Avalonia.Controls;

namespace Luminalium.App.Views;

public partial class PasswordDialogContent : UserControl
{
    public PasswordDialogContent()
    {
        InitializeComponent();
    }

    public PasswordDialogContent(object viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
