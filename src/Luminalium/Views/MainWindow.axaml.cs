using System.ComponentModel;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Contexts;
using FluentAvalonia.UI.Windowing;
using Luminalium.Extensions;
using Luminalium.Services;
using Luminalium.ViewModels;
using SkiaSharp;

namespace Luminalium.Views;

public partial class MainWindow : FAAppWindow
{
    private readonly WorkspaceViewModel _viewModel = new();

    public MainWindow()
    {
        DataContext = _viewModel;
        InitializeComponent();
        ViewInitializer.InitializeView(this);
        _viewModel.ClearRequested += ClearCanvas;
        _viewModel.PropertyChanged += ViewModelChanged;
        WorkspaceCanvas.StrokeCollected += (_, _) => _viewModel.SetStrokeCount(WorkspaceCanvas.Strokes.Count);
        WorkspaceCanvas.StrokeErased += (_, _) => _viewModel.SetStrokeCount(WorkspaceCanvas.Strokes.Count);
        ApplyCanvasSettings();
    }

    private void OpenSettings_OnClick(object? sender, RoutedEventArgs e)
    {
        App.Current.OpenSettingsWindow();
    }

    private void OpenOverlay_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel.StatusText = "悬浮工具将在下一阶段接入";
    }

    private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WorkspaceViewModel.StrokeColor)
            or nameof(WorkspaceViewModel.StrokeWidth)
            or nameof(WorkspaceViewModel.IsEraser)) ApplyCanvasSettings();
    }

    private void ApplyCanvasSettings()
    {
        WorkspaceCanvas.EditingMode = _viewModel.IsEraser
            ? InkCanvasEditingMode.EraseByPoint
            : InkCanvasEditingMode.Ink;
        WorkspaceCanvas.AvaloniaSkiaInkCanvas.Settings.InkColor = SKColor.Parse(_viewModel.StrokeColor);
        WorkspaceCanvas.AvaloniaSkiaInkCanvas.Settings.InkThickness = (float)_viewModel.StrokeWidth;
    }

    private void ClearCanvas(object? sender, EventArgs e)
    {
        foreach (var stroke in WorkspaceCanvas.AvaloniaSkiaInkCanvas.StaticStrokeList.ToArray())
            WorkspaceCanvas.AvaloniaSkiaInkCanvas.RemoveStaticStroke(stroke);
    }

    private async void Save_OnClick(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "luminalium-board.svg",
            DefaultExtension = "svg",
            FileTypeChoices = [new FilePickerFileType("SVG 图像") { Patterns = ["*.svg"] }]
        });
        if (file is null) return;
        var path = await WorkspaceSvgService.ExportAsync(WorkspaceCanvas.Strokes.ToList(), WorkspaceCanvas.Bounds.Width, WorkspaceCanvas.Bounds.Height, file.Path.LocalPath);
        _viewModel.MarkSaved(path);
    }
}
