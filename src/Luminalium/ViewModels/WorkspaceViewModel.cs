using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.Models;

namespace Luminalium.ViewModels;

public sealed partial class WorkspaceViewModel : ObservableObject
{
    public WorkspaceViewModel()
    {
        Colors =
        [
            new WorkspaceColorOption("#E5484D", "朱红"),
            new WorkspaceColorOption("#F59E0B", "琥珀"),
            new WorkspaceColorOption("#16A34A", "翠绿"),
            new WorkspaceColorOption("#0284C7", "天蓝"),
            new WorkspaceColorOption("#7C3AED", "紫罗兰"),
            new WorkspaceColorOption("#171717", "墨黑"),
        ];
        Colors[0].IsSelected = true;
    }

    public ObservableCollection<WorkspaceColorOption> Colors { get; }

    [ObservableProperty] private string strokeColor = "#E5484D";
    [ObservableProperty] private double strokeWidth = 4;
    [ObservableProperty] private bool isEraser;
    [ObservableProperty] private int strokeCount;
    [ObservableProperty] private string statusText = "准备就绪";

    public string ToolModeText => IsEraser ? "橡皮擦" : "画笔";
    public string StrokeCountText => $"{StrokeCount} 条笔迹";

    public event EventHandler? ClearRequested;

    [RelayCommand]
    private void SelectColor(WorkspaceColorOption? option)
    {
        if (option is null) return;
        StrokeColor = option.Hex;
        IsEraser = false;
        foreach (var color in Colors) color.IsSelected = ReferenceEquals(color, option);
    }

    [RelayCommand]
    private void ToggleEraser() => IsEraser = !IsEraser;

    [RelayCommand]
    private void ActivatePen() => IsEraser = false;

    [RelayCommand]
    private void ClearWorkspace()
    {
        ClearRequested?.Invoke(this, EventArgs.Empty);
        SetStrokeCount(0);
        StatusText = "画布已清空";
    }

    public void SetStrokeCount(int count)
    {
        StrokeCount = Math.Max(0, count);
        OnPropertyChanged(nameof(StrokeCountText));
    }

    public void MarkSaved(string? path)
    {
        StatusText = path is null ? "没有可保存的笔迹" : $"已导出 SVG: {Path.GetFileName(path)}";
    }

    partial void OnIsEraserChanged(bool value)
    {
        StrokeWidth = value ? 24 : 4;
        OnPropertyChanged(nameof(ToolModeText));
    }
}
