using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using Luminalium.Presentation;
using Xunit;

namespace Luminalium.Tests;

public sealed class SettingsPhaseTwoTests
{
    [Fact]
    public async Task CoordinatorPersistsAndPublishesOnlySuccessfulUpdates()
    {
        var directory = Path.Combine(Path.GetTempPath(), "LuminaliumTests", Guid.NewGuid().ToString("N"));
        var config = LuminaliumConfig.CreateDefault();
        var service = new ConfigurationService(directory);
        var coordinator = new SettingsCoordinator(config, service);
        var changes = 0;
        coordinator.Changed += (_, _) => changes++;

        Assert.True(await coordinator.UpdateAsync(current => current.Overlay.ToolbarOpacity = 0.5));
        Assert.Equal(0.5, config.Overlay.ToolbarOpacity);
        Assert.Equal(1, changes);
        Assert.Equal(0.5, service.Load().Config.Overlay.ToolbarOpacity);

        Assert.False(await coordinator.UpdateAsync(current => current.Overlay.ToolbarOpacity = 0));
        Assert.Equal(0.5, config.Overlay.ToolbarOpacity);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void ToolbarSnapshotFiltersUnknownIdsAndCompletesMissingOrder()
    {
        var settings = new ToolbarSettings
        {
            ToolbarOrder = ["timer", "unknown", "timer", "clear"],
            DisabledTools = ["timer"],
            ShowClear = false,
            ShowTooltips = false,
            ShowToolbarText = true,
        };

        var snapshot = OverlayToolbarSnapshot.FromSettings(settings);

        Assert.Equal(["timer", "clear", "select", "pen", "eraser", "spotlight", "board_in_board", "apps"], snapshot.Items.Select(item => item.Id));
        Assert.False(snapshot.Items.Single(item => item.Id == "timer").IsVisible);
        Assert.False(snapshot.Items.Single(item => item.Id == "clear").IsVisible);
        Assert.True(snapshot.ShowText);
        Assert.False(snapshot.ShowTooltips);
    }

    [Fact]
    public void PageTurnTimerReadsUpdatedLimitForLaterRequests()
    {
        var limit = 1;
        var timer = new SlidingWindowCommandTimer(() => limit, TimeSpan.FromMinutes(1));

        Assert.True(timer.TryConsumePageTurn().IsSuccess);
        Assert.False(timer.TryConsumePageTurn().IsSuccess);
        limit = 2;
        Assert.True(timer.TryConsumePageTurn().IsSuccess);
    }
}
