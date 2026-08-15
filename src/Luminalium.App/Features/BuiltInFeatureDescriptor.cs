namespace Luminalium.App.Features;

public enum BuiltInFeatureSurface
{
    ShellPage,
    Window,
    Toolbar,
    StatusBar,
}

public enum BuiltInFeatureActivationMode
{
    SharedPage,
    FreshWindow,
    SingletonWindow,
}

public sealed class BuiltInFeatureDescriptor
{
    public BuiltInFeatureDescriptor(
        BuiltInFeatureId id,
        string displayLocalizationKey,
        string iconKey,
        BuiltInFeatureSurface surface,
        BuiltInFeatureActivationMode activationMode,
        string routeKey,
        Func<bool> isAvailable,
        string diagnosticsKey,
        string fallbackDisplayName,
        string fallbackDescription)
    {
        if (!BuiltInFeatureId.TryParseCanonical(id.Value, out var canonicalId) || canonicalId != id)
        {
            throw new ArgumentException("A descriptor requires a canonical built-in feature id.", nameof(id));
        }

        Id = id;
        DisplayLocalizationKey = RequireValue(displayLocalizationKey, nameof(displayLocalizationKey));
        IconKey = iconKey ?? string.Empty;
        Surface = surface;
        ActivationMode = activationMode;
        RouteKey = RequireValue(routeKey, nameof(routeKey));
        IsAvailable = isAvailable ?? throw new ArgumentNullException(nameof(isAvailable));
        DiagnosticsKey = RequireValue(diagnosticsKey, nameof(diagnosticsKey));
        FallbackDisplayName = fallbackDisplayName ?? string.Empty;
        FallbackDescription = fallbackDescription ?? string.Empty;

        if (!StringComparer.Ordinal.Equals(RouteKey, Id.Value))
        {
            throw new ArgumentException("A built-in feature route key must equal its canonical id.", nameof(routeKey));
        }

        if (Surface == BuiltInFeatureSurface.ShellPage && ActivationMode != BuiltInFeatureActivationMode.SharedPage)
        {
            throw new ArgumentException("Shell pages must use shared-page activation.", nameof(activationMode));
        }

        if (Surface != BuiltInFeatureSurface.ShellPage && ActivationMode == BuiltInFeatureActivationMode.SharedPage)
        {
            throw new ArgumentException("Shared-page activation is only valid for shell pages.", nameof(activationMode));
        }
    }

    public BuiltInFeatureId Id { get; }
    public string DisplayLocalizationKey { get; }
    public string IconKey { get; }
    public BuiltInFeatureSurface Surface { get; }
    public BuiltInFeatureActivationMode ActivationMode { get; }
    public string RouteKey { get; }
    public Func<bool> IsAvailable { get; }
    public string DiagnosticsKey { get; }
    public string FallbackDisplayName { get; }
    public string FallbackDescription { get; }

    private static string RequireValue(string? value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty.", parameterName)
            : value;
}
