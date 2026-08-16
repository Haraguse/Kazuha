using Luminalium.Core.Configuration;

namespace Luminalium.App.Services;

public sealed record OverlayToolbarItem(string Id, bool IsVisible);

public sealed record OverlayToolbarSnapshot(
    IReadOnlyList<OverlayToolbarItem> Items,
    bool ShowText,
    bool ShowTooltips)
{
    private static readonly string[] DefaultOrder =
    [
        "select", "pen", "eraser", "spotlight", "board_in_board", "timer", "clear", "apps",
    ];

    public static OverlayToolbarSnapshot FromSettings(ToolbarSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var disabled = settings.DisabledTools
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var known = DefaultOrder.ToHashSet(StringComparer.Ordinal);
        var ordered = settings.ToolbarOrder
            .Where(known.Contains)
            .Distinct(StringComparer.Ordinal)
            .Concat(DefaultOrder.Where(id => !settings.ToolbarOrder.Contains(id, StringComparer.Ordinal)));
        return new OverlayToolbarSnapshot(
            ordered.Select(id => new OverlayToolbarItem(id, IsVisible(id, settings, disabled))).ToArray(),
            settings.ShowToolbarText,
            settings.ShowTooltips);
    }

    private static bool IsVisible(string id, ToolbarSettings settings, HashSet<string> disabled) =>
        !disabled.Contains(id) && id switch
        {
            "clear" => settings.ShowClear,
            "spotlight" => settings.ShowSpotlight,
            "board_in_board" => settings.ShowBoardInBoard,
            "timer" => settings.ShowTimer,
            _ => true,
        };
}
