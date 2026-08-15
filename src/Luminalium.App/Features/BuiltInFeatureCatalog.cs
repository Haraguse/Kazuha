using System.Collections.ObjectModel;

namespace Luminalium.App.Features;

public sealed class BuiltInFeatureCatalog
{
    private readonly System.Collections.ObjectModel.ReadOnlyCollection<BuiltInFeatureDescriptor> _descriptors;
    private readonly System.Collections.ObjectModel.ReadOnlyDictionary<BuiltInFeatureId, BuiltInFeatureDescriptor> _byId;

    public BuiltInFeatureCatalog(IEnumerable<BuiltInFeatureDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var ordered = descriptors.ToArray();
        var byId = new Dictionary<BuiltInFeatureId, BuiltInFeatureDescriptor>();
        foreach (var descriptor in ordered)
        {
            if (descriptor is null)
            {
                throw new ArgumentException("A built-in feature descriptor cannot be null.", nameof(descriptors));
            }

            if (!byId.TryAdd(descriptor.Id, descriptor))
            {
                throw new ArgumentException(
                    $"Duplicate built-in feature id '{descriptor.Id.Value}'.",
                    nameof(descriptors));
            }
        }

        _descriptors = Array.AsReadOnly(ordered);
        _byId = new ReadOnlyDictionary<BuiltInFeatureId, BuiltInFeatureDescriptor>(byId);
    }

    public static BuiltInFeatureCatalog Default { get; } = CreateDefault();

    public int Count => _descriptors.Count;
    public IReadOnlyList<BuiltInFeatureDescriptor> Descriptors => _descriptors;

    public bool TryGet(BuiltInFeatureId id, out BuiltInFeatureDescriptor descriptor) =>
        _byId.TryGetValue(id, out descriptor!);

    private static BuiltInFeatureCatalog CreateDefault() => new(
    [
        Descriptor(BuiltInFeatureId.Settings, "", "settings.svg", BuiltInFeatureSurface.ShellPage, BuiltInFeatureActivationMode.SharedPage, "Luminalium 的核心设置插件。"),
        Descriptor(BuiltInFeatureId.Onboarding, "Onboarding", "", BuiltInFeatureSurface.ShellPage, BuiltInFeatureActivationMode.SharedPage, ""),
        Descriptor(BuiltInFeatureId.Board, "板中板 - Luminalium", "board-in-board.svg", BuiltInFeatureSurface.Window, BuiltInFeatureActivationMode.FreshWindow, "自由书写的黑板"),
        Descriptor(BuiltInFeatureId.Timer, "Timer", "timer.svg", BuiltInFeatureSurface.Toolbar, BuiltInFeatureActivationMode.FreshWindow, "Luminalium 的定时器插件。"),
        Descriptor(BuiltInFeatureId.Spotlight, "Spotlight", "spotlight.svg", BuiltInFeatureSurface.Toolbar, BuiltInFeatureActivationMode.FreshWindow, "Luminalium 的聚光灯插件，支持区域高亮、关灯模式和放大镜。"),
        Descriptor(BuiltInFeatureId.AppLauncher, "App Launcher", "apps.svg", BuiltInFeatureSurface.Toolbar, BuiltInFeatureActivationMode.FreshWindow, "Launch applications from the toolbar."),
        Descriptor(BuiltInFeatureId.Logs, "日志 - Luminalium", "debug.svg", BuiltInFeatureSurface.ShellPage, BuiltInFeatureActivationMode.SharedPage, "查看应用日志和系统信息"),
        Descriptor(BuiltInFeatureId.StatusBar, "Status Bar", "", BuiltInFeatureSurface.StatusBar, BuiltInFeatureActivationMode.FreshWindow, "Luminalium 的状态栏插件。"),
    ]);

    private static BuiltInFeatureDescriptor Descriptor(
        BuiltInFeatureId id,
        string fallbackDisplayName,
        string iconKey,
        BuiltInFeatureSurface surface,
        BuiltInFeatureActivationMode activationMode,
        string fallbackDescription) =>
        new(
            id,
            $"Plugin.{id.Value}.Name",
            iconKey,
            surface,
            activationMode,
            id.Value,
            static () => true,
            $"built-in-feature.{id.Value}",
            fallbackDisplayName,
            fallbackDescription);
}
