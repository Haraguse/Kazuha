using Avalonia.Controls;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class StatusBarWindow : Window
{
    private readonly StatusBarViewModel _viewModel;

    public StatusBarWindow()
        : this(new StatusBarViewModel())
    {
    }

    public StatusBarWindow(StatusBarViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        Opened += async (_, _) => await _viewModel.RefreshCommand.ExecuteAsync(null);
    }

    private void OnCloseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
