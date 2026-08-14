using Avalonia.Controls;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class UpdateDialogContent : UserControl
{
    public UpdateDialogContent()
        : this(new UpdateDialogViewModel())
    {
    }

    public UpdateDialogContent(UpdateDialogViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
