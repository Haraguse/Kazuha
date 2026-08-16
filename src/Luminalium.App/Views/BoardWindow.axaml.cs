using System.ComponentModel;
using Avalonia.Platform.Storage;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Contexts;
using FluentAvalonia.UI.Windowing;
using Luminalium.App.Board;
using Luminalium.App.ViewModels;
using SkiaSharp;

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

        ApplyEditingState();

        BoardCanvas.StrokeCollected += OnStrokeCollected;
        BoardCanvas.StrokeErased += OnStrokeErased;
        _viewModel.ClearRequested += OnClearRequested;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BoardViewModel.StrokeColor)
            or nameof(BoardViewModel.StrokeWidth)
            or nameof(BoardViewModel.IsEraser))
        {
            ApplyEditingState();
        }
    }

    /// <summary>Pushes the viewmodel's pen/eraser state onto the InkCanvas.</summary>
    private void ApplyEditingState()
    {
        BoardCanvas.EditingMode = _viewModel.IsEraser
            ? InkCanvasEditingMode.EraseByPoint
            : InkCanvasEditingMode.Ink;

        var settings = BoardCanvas.AvaloniaSkiaInkCanvas.Settings;
        settings.InkColor = SKColor.Parse(_viewModel.StrokeColor);
        settings.InkThickness = (float)_viewModel.StrokeWidth;
    }

    private void OnStrokeCollected(object? sender, AvaloniaSkiaInkCanvasStrokeCollectedEventArgs e)
        => _viewModel.SetStrokeCount(BoardCanvas.Strokes.Count);

    private void OnStrokeErased(object? sender, ErasingCompletedEventArgs e)
        => _viewModel.SetStrokeCount(BoardCanvas.Strokes.Count);

    private void OnClearRequested(object? sender, EventArgs e)
    {
        var ink = BoardCanvas.AvaloniaSkiaInkCanvas;
        foreach (var stroke in ink.StaticStrokeList.ToArray())
        {
            ink.RemoveStaticStroke(stroke);
        }

        _viewModel.SetStrokeCount(0);
    }

    private async void OnSaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = _viewModel.Localization["Board.Save.SuggestedFile"],
            DefaultExtension = "svg",
            FileTypeChoices =
            [
                new FilePickerFileType("SVG") { Patterns = ["*.svg"] },
            ],
        });

        if (file is null)
        {
            return;
        }

        var result = await BoardSaveService.SaveAsync(
            BoardCanvas.Strokes.ToList(),
            BoardCanvas.Bounds.Width,
            BoardCanvas.Bounds.Height,
            file.Path.LocalPath);

        _viewModel.HandleSaveResult(result);
    }

    private void OnCloseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    /// <summary>Opens the pen color palette when the pen tool is double-tapped.</summary>
    private void OnPenDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (PenButton.Flyout is { } flyout)
        {
            flyout.ShowAt(PenButton);
        }
    }
}