#if !LUMINALIUM_HEADLESS_TESTS
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
#endif

namespace Luminalium.App.Overlay;

public readonly record struct OverlayScreenBounds(int X, int Y, double Width, double Height, double Scaling)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

public interface IOverlayScreenProvider
{
    IReadOnlyList<OverlayScreenBounds> GetScreens();

    OverlayScreenBounds GetScreen(int? screenIndex = null);
}

#if !LUMINALIUM_HEADLESS_TESTS
public sealed class AvaloniaOverlayScreenProvider : IOverlayScreenProvider
{
    private static readonly OverlayScreenBounds FallbackScreen = new(0, 0, 1280, 720, 1.0);
    private readonly Window _owner;

    public AvaloniaOverlayScreenProvider(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
    }

    public IReadOnlyList<OverlayScreenBounds> GetScreens()
    {
        var screens = _owner.Screens.All.Select(ToBounds).ToArray();
        return screens.Length == 0 ? [FallbackScreen] : screens;
    }

    public OverlayScreenBounds GetScreen(int? screenIndex = null)
    {
        var screens = GetScreens();
        if (screenIndex is int index && index >= 0 && index < screens.Count)
        {
            return screens[index];
        }

        var ownerScreen = _owner.Screens.ScreenFromWindow(_owner);
        if (ownerScreen is not null)
        {
            return ToBounds(ownerScreen);
        }

        var primary = _owner.Screens.Primary;
        return primary is null ? screens[0] : ToBounds(primary);
    }

    private static OverlayScreenBounds ToBounds(Screen screen)
    {
        var scaling = screen.Scaling <= 0 ? 1.0 : screen.Scaling;
        return new OverlayScreenBounds(
            screen.Bounds.X,
            screen.Bounds.Y,
            screen.Bounds.Width / scaling,
            screen.Bounds.Height / scaling,
            scaling);
    }
}
#endif
