using Avalonia.Controls;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class TimerWindow : Window
{
    private readonly TimerViewModel _viewModel;

    public TimerWindow()
        : this(new TimerViewModel())
    {
    }

    public TimerWindow(TimerViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        Closed += (_, _) => _viewModel.Terminate();
    }

    private void OnCloseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.Terminate();
        Close();
    }
}
