using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Windowing;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Views;

public partial class BoardWindow : FAAppWindow
{
    private readonly BoardViewModel _viewModel;

    public BoardWindow()
        : this(new BoardViewModel())
    {
    }

    public BoardWindow(BoardViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.Height = 44;

        BoardCanvas.AnnotationSink = new BoardGestureSink(_viewModel);
    }

    private async void OnSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = _viewModel.Localization["Board.Save.SuggestedFile"],
            DefaultExtension = "json",
            FileTypeChoices =
            [
                new FilePickerFileType("JSON") { Patterns = ["*.json"] },
            ],
        });

        if (file is null)
        {
            return;
        }

        await _viewModel.SaveAsync(file.Path.LocalPath);
    }

    private void OnCloseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private sealed class BoardGestureSink(BoardViewModel viewModel) : Luminalium.App.Overlay.IAnnotationSink
    {
        public void RecordStroke(Luminalium.App.Overlay.StrokeModel stroke) =>
            viewModel.RecordGesture(stroke);

        public void ClearStrokes() =>
            viewModel.ClearBoardCommand.Execute(null);
    }
}
