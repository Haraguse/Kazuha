using Avalonia.Controls;
using Luminalium.App.Features;
using Luminalium.App.Views;

namespace Luminalium.App.Services;

public static class NativeBuiltInFeatureHostFactory
{
    public static BuiltInFeatureHost Create(Func<Window> ownerProvider) =>
        new(
            BuiltInFeatureCatalog.Default,
            new AvaloniaBuiltInFeatureDispatcher(),
            new Dictionary<BuiltInFeatureId, IBuiltInFeatureFactory>
            {
                [BuiltInFeatureId.Board] = new AvaloniaBuiltInFeatureFactory(ownerProvider, static () => new BoardWindow()),
                [BuiltInFeatureId.Timer] = new AvaloniaBuiltInFeatureFactory(ownerProvider, static () => new TimerWindow()),
                [BuiltInFeatureId.Spotlight] = new AvaloniaBuiltInFeatureFactory(ownerProvider, static () => new SpotlightWindow()),
                [BuiltInFeatureId.AppLauncher] = new AvaloniaBuiltInFeatureFactory(ownerProvider, static () => new AppLauncherWindow()),
                [BuiltInFeatureId.StatusBar] = new AvaloniaBuiltInFeatureFactory(ownerProvider, static () => new StatusBarWindow()),
            });
}
