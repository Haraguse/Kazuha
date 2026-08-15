using Avalonia.Controls;

namespace Luminalium.App.Views;

public partial class RetryCloseDialogContent : UserControl
{
    public RetryCloseDialogContent()
    {
        InitializeComponent();
    }

    public RetryCloseDialogContent(object viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
