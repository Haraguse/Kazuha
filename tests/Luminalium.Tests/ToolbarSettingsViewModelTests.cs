using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Luminalium.Core.Configuration;
using Xunit;

namespace Luminalium.Tests;

public sealed class ToolbarSettingsViewModelTests
{
    [Fact]
    public void ConstructorMapsOverlayFields()
    {
        var config = new LuminaliumConfig();
        config.Overlay.ClearMode = ClearMode.Button;
        config.Overlay.ToolbarOpacity = 0.42;
        config.Overlay.SyncOpacity = true;

        var vm = new ToolbarSettingsViewModel(config);

        Assert.Equal(1, vm.SelectedClearModeIndex);
        Assert.Equal(0.42, vm.ToolbarOpacity, precision: 2);
        Assert.True(vm.SyncOpacity);
    }

    [Fact]
    public void ConstructorMapsDisabledTools()
    {
        var config = new LuminaliumConfig();
        config.Toolbar.DisabledTools = ["clear", "timer"];

        var vm = new ToolbarSettingsViewModel(config);

        Assert.Equal(8, vm.Tools.Count);
        Assert.True(vm.Tools.Single(item => item.Id == "clear").IsDisabled);
        Assert.True(vm.Tools.Single(item => item.Id == "timer").IsDisabled);
        Assert.False(vm.Tools.Single(item => item.Id == "pen").IsDisabled);
    }

    [Fact]
    public void ConstructorMapsToolOrderCompletingMissingAndFilteringUnknown()
    {
        var config = new LuminaliumConfig();
        config.Toolbar.ToolbarOrder = ["timer", "unknown", "clear"];

        var vm = new ToolbarSettingsViewModel(config);

        Assert.Equal(
            ["timer", "clear", "select", "pen", "eraser", "spotlight", "board_in_board", "apps"],
            vm.ToolOrderItems.Select(item => item.Id));
    }

    [Fact]
    public void ConstructorMapsQuickLaunchAppsSkippingBlankEntries()
    {
        var config = new LuminaliumConfig();
        config.Toolbar.QuickLaunchApps = ["a.exe", "", "b.exe"];

        var vm = new ToolbarSettingsViewModel(config);

        Assert.Equal(["a.exe", "b.exe"], vm.QuickLaunchApps);
    }

    [Fact]
    public void MoveToolUpAndDownReorderToolOrderItems()
    {
        var vm = new ToolbarSettingsViewModel(new LuminaliumConfig());
        var clear = vm.ToolOrderItems.Single(item => item.Id == "clear");
        var originalIndex = vm.ToolOrderItems.IndexOf(clear);

        vm.MoveToolUpCommand.Execute(clear);
        Assert.Equal(originalIndex - 1, vm.ToolOrderItems.IndexOf(clear));

        vm.MoveToolDownCommand.Execute(clear);
        Assert.Equal(originalIndex, vm.ToolOrderItems.IndexOf(clear));
    }

    [Fact]
    public void MoveToolAtBoundaryIsIgnored()
    {
        var vm = new ToolbarSettingsViewModel(new LuminaliumConfig());
        var first = vm.ToolOrderItems[0];
        var last = vm.ToolOrderItems[^1];

        vm.MoveToolUpCommand.Execute(first);
        vm.MoveToolDownCommand.Execute(last);

        Assert.Equal(0, vm.ToolOrderItems.IndexOf(first));
        Assert.Equal(vm.ToolOrderItems.Count - 1, vm.ToolOrderItems.IndexOf(last));
    }

    [Fact]
    public void AddQuickLaunchAppAddsAndDeduplicatesCaseInsensitively()
    {
        var vm = new ToolbarSettingsViewModel(new LuminaliumConfig());

        vm.NewQuickLaunchApp = "notepad.exe";
        vm.AddQuickLaunchAppCommand.Execute(null);
        Assert.Equal(["notepad.exe"], vm.QuickLaunchApps);
        Assert.Equal(string.Empty, vm.NewQuickLaunchApp);

        vm.NewQuickLaunchApp = "NOTEPAD.EXE";
        vm.AddQuickLaunchAppCommand.Execute(null);
        Assert.Single(vm.QuickLaunchApps);
    }

    [Fact]
    public void RemoveQuickLaunchAppRemovesMatchingEntry()
    {
        var config = new LuminaliumConfig();
        config.Toolbar.QuickLaunchApps = ["a.exe", "b.exe"];
        var vm = new ToolbarSettingsViewModel(config);

        vm.RemoveQuickLaunchAppCommand.Execute("a.exe");

        Assert.Equal(["b.exe"], vm.QuickLaunchApps);
    }

    [Fact]
    public void ToolbarSnapshotAppliesDisabledToolsAndCompletesOrder()
    {
        var settings = new ToolbarSettings
        {
            ToolbarOrder = ["clear", "apps"],
            DisabledTools = ["apps"],
            ShowClear = false,
        };

        var snapshot = OverlayToolbarSnapshot.FromSettings(settings);

        Assert.Equal(
            ["clear", "apps", "select", "pen", "eraser", "spotlight", "board_in_board", "timer"],
            snapshot.Items.Select(item => item.Id));
        Assert.False(snapshot.Items.Single(item => item.Id == "clear").IsVisible);
        Assert.False(snapshot.Items.Single(item => item.Id == "apps").IsVisible);
        Assert.True(snapshot.Items.Single(item => item.Id == "select").IsVisible);
    }
}