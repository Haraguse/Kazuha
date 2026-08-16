using Avalonia.Controls;
using Luminalium.App.Features;
using Luminalium.App.Views;

namespace Luminalium.App.Services;

public static class NativeBuiltInFeatureHostFactory
{
    public static BuiltInFeatureHost Create() =>
        new(
            BuiltInFeatureCatalog.Default,
            new AvaloniaBuiltInFeatureDispatcher(),
            new Dictionary<BuiltInFeatureId, IBuiltInFeatureFactory>
            {
                [BuiltInFeatureId.Board] = new AvaloniaBuiltInFeatureFactory(static () => new BoardWindow()),
                [BuiltInFeatureId.Timer] = new AvaloniaBuiltInFeatureFactory(static () => new TimerWindow()),
                [BuiltInFeatureId.Spotlight] = new AvaloniaBuiltInFeatureFactory(static () => new SpotlightWindow()),
                [BuiltInFeatureId.AppLauncher] = new AvaloniaBuiltInFeatureFactory(static () => new AppLauncherWindow()),
                [BuiltInFeatureId.StatusBar] = new AvaloniaBuiltInFeatureFactory(static () => new StatusBarWindow()),
            });
}
