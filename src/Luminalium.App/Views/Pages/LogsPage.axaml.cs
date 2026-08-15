using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views.Pages;

public partial class LogsPage : UserControl
{
    private LogsViewModel ViewModel => (LogsViewModel)DataContext!;

    public LogsPage()
    {
        InitializeComponent();
    }

    private async void OnExportClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "luminalium-logs.jsonl",
            DefaultExtension = "jsonl",
            FileTypeChoices = [new FilePickerFileType("JSON Lines") { Patterns = ["*.jsonl"] }],
        });
        var path = file?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path))
        {
            ViewModel.ExportCommand.Execute(path);
        }
    }
}
