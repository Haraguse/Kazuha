using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Luminalium.App.ViewModels;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Settings labels are instance members so compiled bindings resolve consistently.")]
public sealed partial class ToolbarSettingsViewModel : ObservableObject
{
    private readonly SettingsCoordinator? _coordinator;
    private bool _initializing;

    private static readonly (string Id, string Label)[] KnownToolDefinitions =
    [
        ("select", "选择"),
        ("pen", "画笔"),
        ("eraser", "橡皮"),
        ("spotlight", "聚光灯"),
        ("board_in_board", "板中板"),
        ("timer", "计时器"),
        ("clear", "清除"),
        ("apps", "快捷应用"),
    ];

    public ToolbarSettingsViewModel(LuminaliumConfig? config = null, SettingsCoordinator? coordinator = null)
    {
        _coordinator = coordinator;
        var toolbar = config?.Toolbar ?? new ToolbarSettings();
        var overlay = config?.Overlay ?? new OverlaySettings();
        _initializing = true;

        showClear = toolbar.ShowClear;
        showSpotlight = toolbar.ShowSpotlight;
        showBoardInBoard = toolbar.ShowBoardInBoard;
        showTimer = toolbar.ShowTimer;
        showToolbarText = toolbar.ShowToolbarText;
        showTooltips = toolbar.ShowTooltips;

        selectedClearModeIndex = (int)overlay.ClearMode;
        toolbarOpacity = overlay.ToolbarOpacity;
        syncOpacity = overlay.SyncOpacity;

        var disabled = toolbar.DisabledTools
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        if (!toolbar.ShowClear) disabled.Add("clear");
        if (!toolbar.ShowSpotlight) disabled.Add("spotlight");
        if (!toolbar.ShowBoardInBoard) disabled.Add("board_in_board");
        if (!toolbar.ShowTimer) disabled.Add("timer");
        foreach (var (id, label) in KnownToolDefinitions)
        {
            Tools.Add(new ToolAvailabilityItem(this, id, label, disabled.Contains(id)));
        }

        var labelsById = KnownToolDefinitions.ToDictionary(item => item.Id, item => item.Label, StringComparer.Ordinal);
        var orderedIds = toolbar.ToolbarOrder
            .Where(labelsById.ContainsKey)
            .Distinct(StringComparer.Ordinal)
            .Concat(labelsById.Keys.Where(id => !toolbar.ToolbarOrder.Contains(id, StringComparer.Ordinal)));
        foreach (var id in orderedIds)
        {
            ToolOrderItems.Add(new ToolOrderItem(id, labelsById[id]));
        }

        foreach (var app in toolbar.QuickLaunchApps)
        {
            if (!string.IsNullOrWhiteSpace(app))
            {
                QuickLaunchApps.Add(app);
            }
        }

        _initializing = false;
    }

    [ObservableProperty]
    private bool showClear;

    [ObservableProperty]
    private bool showSpotlight;

    [ObservableProperty]
    private bool showBoardInBoard;

    [ObservableProperty]
    private bool showTimer;

    [ObservableProperty]
    private bool showToolbarText;

    [ObservableProperty]
    private bool showTooltips;

    [ObservableProperty]
    private int selectedClearModeIndex;

    [ObservableProperty]
    private double toolbarOpacity = 1.0;

    [ObservableProperty]
    private bool syncOpacity;

    [ObservableProperty]
    private string newQuickLaunchApp = string.Empty;

    public ObservableCollection<ToolAvailabilityItem> Tools { get; } = [];

    public ObservableCollection<ToolOrderItem> ToolOrderItems { get; } = [];

    public ObservableCollection<string> QuickLaunchApps { get; } = [];

    public string PageTitle => "工具栏";

    public string VisibilityTitle => "工具显示";

    public string DisplayTitle => "工具栏显示";

    public string DisplayDescription => "控制 Overlay 工具栏是否显示文字和提示。";

    public string ToolVisibilityDescription => "选择需要显示在 Overlay 工具栏中的工具。";

    public string ToolbarOpacityDisplay => $"{ToolbarOpacity:P0}";

    public string ClearAndOpacityTitle => "清屏和透明度";

    public string ClearModeLabel => "清屏方式";
    public string ClearModeDescription => "选择 Overlay 清屏时使用的动效方式。";
    public string ToolbarOpacityLabel => "工具栏不透明度";
    public string ToolbarOpacityDescription => "调整 Overlay 工具栏的整体不透明度。";
    public string SyncOpacityLabel => "同步不透明度";
    public string SyncOpacityDescription => "让侧边页面与工具栏保持相同的不透明度。";

    public string AvailabilityTitle => "工具可用性";
    public string AvailabilityDescription => "关闭的工具将从 Overlay 工具栏中隐藏。";

    public string OrderTitle => "工具顺序";
    public string OrderDescription => "调整工具在 Overlay 工具栏中的排列顺序。";
    public string MoveUpLabel => "上移";
    public string MoveDownLabel => "下移";

    public string QuickLaunchTitle => "快速启动应用";
    public string QuickLaunchDescription => "在工具栏中添加可快速启动的应用路径。";
    public string QuickLaunchPlaceholder => "应用路径，例如 C:\\Windows\\Notepad.exe";
    public string AddLabel => "添加";
    public string RemoveLabel => "移除";
    public string EmptyQuickLaunchText => "暂无快捷应用";

    public IReadOnlyList<string> ClearModes { get; } = ["上滑清除", "按钮清除"];

    public string ShowClearLabel => "清除";
    public string ShowSpotlightLabel => "聚光灯";
    public string ShowBoardInBoardLabel => "板中板";
    public string ShowTimerLabel => "计时器";
    public string ShowToolbarTextLabel => "显示工具文字";
    public string ShowTooltipsLabel => "显示工具提示";

    partial void OnShowClearChanged(bool value) => Persist(c => c.Toolbar.ShowClear = value);
    partial void OnShowSpotlightChanged(bool value) => Persist(c => c.Toolbar.ShowSpotlight = value);
    partial void OnShowBoardInBoardChanged(bool value) => Persist(c => c.Toolbar.ShowBoardInBoard = value);
    partial void OnShowTimerChanged(bool value) => Persist(c => c.Toolbar.ShowTimer = value);
    partial void OnShowToolbarTextChanged(bool value) => Persist(c => c.Toolbar.ShowToolbarText = value);
    partial void OnShowTooltipsChanged(bool value) => Persist(c => c.Toolbar.ShowTooltips = value);

    partial void OnSelectedClearModeIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(ClearMode), value)) Persist(c => c.Overlay.ClearMode = (ClearMode)value);
    }

    partial void OnToolbarOpacityChanged(double value)
    {
        OnPropertyChanged(nameof(ToolbarOpacityDisplay));
        Persist(c => c.Overlay.ToolbarOpacity = value);
    }

    partial void OnSyncOpacityChanged(bool value) => Persist(c => c.Overlay.SyncOpacity = value);

    public void SetToolDisabled(string id, bool disabled)
    {
        if (_initializing)
        {
            return;
        }

        var disabledTools = Tools
            .Where(item => item.IsDisabled)
            .Select(item => item.Id)
            .ToArray();
        Persist(c =>
        {
            c.Toolbar.DisabledTools = disabledTools;
            switch (id)
            {
                case "clear":
                    c.Toolbar.ShowClear = !disabled;
                    break;
                case "spotlight":
                    c.Toolbar.ShowSpotlight = !disabled;
                    break;
                case "board_in_board":
                    c.Toolbar.ShowBoardInBoard = !disabled;
                    break;
                case "timer":
                    c.Toolbar.ShowTimer = !disabled;
                    break;
            }
        });
    }

    [RelayCommand]
    private void MoveToolUp(ToolOrderItem? item)
    {
        if (item is null)
        {
            return;
        }

        var index = ToolOrderItems.IndexOf(item);
        if (index <= 0)
        {
            return;
        }

        ToolOrderItems.Move(index, index - 1);
        PersistToolOrder();
    }

    [RelayCommand]
    private void MoveToolDown(ToolOrderItem? item)
    {
        if (item is null)
        {
            return;
        }

        var index = ToolOrderItems.IndexOf(item);
        if (index < 0 || index >= ToolOrderItems.Count - 1)
        {
            return;
        }

        ToolOrderItems.Move(index, index + 1);
        PersistToolOrder();
    }

    [RelayCommand]
    private void AddQuickLaunchApp()
    {
        var app = NewQuickLaunchApp;
        if (string.IsNullOrWhiteSpace(app))
        {
            return;
        }

        if (QuickLaunchApps.Contains(app, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        QuickLaunchApps.Add(app);
        NewQuickLaunchApp = string.Empty;
        PersistQuickLaunchApps();
    }

    [RelayCommand]
    private void RemoveQuickLaunchApp(string? app)
    {
        if (app is null)
        {
            return;
        }

        QuickLaunchApps.Remove(app);
        PersistQuickLaunchApps();
    }

    private void PersistToolOrder() => Persist(c => c.Toolbar.ToolbarOrder = ToolOrderItems.Select(item => item.Id).ToArray());

    private void PersistQuickLaunchApps() => Persist(c => c.Toolbar.QuickLaunchApps = QuickLaunchApps.ToArray());

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null)
        {
            _ = _coordinator.UpdateAsync(update);
        }
    }
}
