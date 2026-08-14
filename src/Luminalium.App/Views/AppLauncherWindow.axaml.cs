using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class AppLauncherWindow : Window
{
    private readonly AppLauncherViewModel _viewModel;

    public AppLauncherWindow()
        : this(new AppLauncherViewModel())
    {
    }

    public AppLauncherWindow(AppLauncherViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void OnAddClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var file = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = _viewModel.Localization["Launcher.Picker.Title"],
            AllowMultiple = false,
        });

        if (file is null || file.Count == 0)
        {
            return;
        }

        var path = file[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _viewModel.AddEntry(System.IO.Path.GetFileNameWithoutExtension(path), path);
    }

    private void OnRemoveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var entry = _viewModel.SelectedEntry;
        if (entry is not null)
        {
            _viewModel.RemoveEntry(entry.Path);
        }
    }

    private void OnCloseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();
}
