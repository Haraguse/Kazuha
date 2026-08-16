using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Luminalium.App.ViewModels;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Settings labels are bound to the UI as instance members so compiled bindings resolve consistently.")]
public sealed partial class PenSettingsViewModel : ObservableObject
{
    private readonly SettingsCoordinator? _coordinator;
    private bool _initializing;

    private static readonly string[] PenColorHexes = ["#000000", "#FF0000", "#FF8C00", "#FFD700", "#008000", "#0000FF", "#800080", "#FFFFFF"];
    private static readonly string[] HighlightColorHexes = ["#FFFF00", "#00FF00", "#00FFFF", "#FF69B4", "#0000FF", "#800080"];

    public PenSettingsViewModel(LuminaliumConfig? config = null, SettingsCoordinator? coordinator = null)
    {
        _coordinator = coordinator;
        var pen = config?.SelfPen ?? new SelfPenSettings();
        var ppt = config?.Ppt ?? new PptSettings();
        _initializing = true;
        selectedPenModeIndex = (int)ppt.PenMode;
        penWidth = pen.PenWidth;
        highlightWidth = pen.HighlightWidth;
        eraserWidth = pen.EraserWidth;
        highlightOpacity = pen.HighlightOpacity;
        selectedPenColorIndex = Array.IndexOf(PenColorHexes, pen.PenColor);
        if (selectedPenColorIndex < 0) selectedPenColorIndex = 0;
        selectedHighlightColorIndex = Array.IndexOf(HighlightColorHexes, pen.HighlightColor);
        if (selectedHighlightColorIndex < 0) selectedHighlightColorIndex = 0;
        selectedFrameRateModeIndex = (int)pen.FrameRateMode;
        selectedPenEffectIndex = (int)pen.PenEffect;
        palmErase = pen.PalmErase;
        _initializing = false;
    }

    [ObservableProperty]
    private int selectedPenModeIndex;

    [ObservableProperty]
    private int penWidth;

    [ObservableProperty]
    private int highlightWidth;

    [ObservableProperty]
    private int eraserWidth;

    [ObservableProperty]
    private double highlightOpacity;

    [ObservableProperty]
    private int selectedPenColorIndex;

    [ObservableProperty]
    private int selectedHighlightColorIndex;

    [ObservableProperty]
    private int selectedFrameRateModeIndex;

    [ObservableProperty]
    private int selectedPenEffectIndex;

    [ObservableProperty]
    private bool palmErase;

    public string PageTitle => "画笔";

    public string PenModeSectionTitle => "画笔模式";

    public string PenModeLabel => "画笔驱动模式";

    public string PenSectionTitle => "画笔";

    public string PenWidthLabel => "画笔粗细";
    public string PenColorLabel => "画笔颜色";

    public string HighlightSectionTitle => "荧光笔";

    public string HighlightWidthLabel => "荧光笔粗细";
    public string HighlightOpacityLabel => "荧光笔不透明度";
    public string HighlightColorLabel => "荧光笔颜色";

    public string EraserSectionTitle => "橡皮";

    public string EraserWidthLabel => "橡皮粗细";
    public string PalmEraseLabel => "手掌擦除";
    public string PalmEraseDescription => "用手掌触碰屏幕时擦除当前笔画";

    public string PerformanceSectionTitle => "性能";

    public string FrameRateModeLabel => "采样帧率";
    public string PenEffectLabel => "笔迹效果";

    public IReadOnlyList<string> PenModes { get; } = ["COM", "自研"];

    public IReadOnlyList<string> PenColors { get; } = ["黑色", "红色", "橙色", "黄色", "绿色", "蓝色", "紫色", "白色"];

    public IReadOnlyList<string> HighlightColors { get; } = ["黄色", "绿色", "青色", "粉色", "蓝色", "紫色"];

    public IReadOnlyList<string> FrameRateModes { get; } = ["低", "自适应", "高"];

    public IReadOnlyList<string> PenEffects { get; } = ["关闭", "有限", "完整"];

    public string PenWidthDisplay => PenWidth.ToString(CultureInfo.InvariantCulture);
    public string HighlightWidthDisplay => HighlightWidth.ToString(CultureInfo.InvariantCulture);
    public string EraserWidthDisplay => EraserWidth.ToString(CultureInfo.InvariantCulture);
    public string HighlightOpacityDisplay => HighlightOpacity.ToString("0.##", CultureInfo.InvariantCulture);

    partial void OnSelectedPenModeIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(PenMode), value)) Persist(c => c.Ppt.PenMode = (PenMode)value);
    }

    partial void OnPenWidthChanged(int value)
    {
        OnPropertyChanged(nameof(PenWidthDisplay));
        Persist(c => c.SelfPen.PenWidth = value);
    }

    partial void OnHighlightWidthChanged(int value)
    {
        OnPropertyChanged(nameof(HighlightWidthDisplay));
        Persist(c => c.SelfPen.HighlightWidth = value);
    }

    partial void OnEraserWidthChanged(int value)
    {
        OnPropertyChanged(nameof(EraserWidthDisplay));
        Persist(c => c.SelfPen.EraserWidth = value);
    }

    partial void OnHighlightOpacityChanged(double value)
    {
        OnPropertyChanged(nameof(HighlightOpacityDisplay));
        Persist(c => c.SelfPen.HighlightOpacity = value);
    }

    partial void OnSelectedPenColorIndexChanged(int value)
    {
        if (value >= 0 && value < PenColorHexes.Length) Persist(c => c.SelfPen.PenColor = PenColorHexes[value]);
    }

    partial void OnSelectedHighlightColorIndexChanged(int value)
    {
        if (value >= 0 && value < HighlightColorHexes.Length) Persist(c => c.SelfPen.HighlightColor = HighlightColorHexes[value]);
    }

    partial void OnSelectedFrameRateModeIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(PenFrameRateMode), value)) Persist(c => c.SelfPen.FrameRateMode = (PenFrameRateMode)value);
    }

    partial void OnSelectedPenEffectIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(PenEffect), value)) Persist(c => c.SelfPen.PenEffect = (PenEffect)value);
    }

    partial void OnPalmEraseChanged(bool value) => Persist(c => c.SelfPen.PalmErase = value);

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null)
        {
            _ = _coordinator.UpdateAsync(update);
        }
    }
}