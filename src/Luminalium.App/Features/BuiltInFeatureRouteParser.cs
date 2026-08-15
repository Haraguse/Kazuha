namespace Luminalium.App.Features;

public sealed class BuiltInFeatureRouteParser
{
    private const string PluginPrefix = "plugin:";
    private const string FeaturePrefix = "feature:";
    private readonly BuiltInFeatureCatalog _catalog;

    public BuiltInFeatureRouteParser(BuiltInFeatureCatalog? catalog = null)
    {
        _catalog = catalog ?? BuiltInFeatureCatalog.Default;
    }

    public BuiltInFeatureActivationResult Parse(string? route, string? correlationId = null)
    {
        var sourceRoute = route ?? string.Empty;
        if (string.IsNullOrWhiteSpace(route))
        {
            return NotFound(sourceRoute, correlationId);
        }

        var candidate = route switch
        {
            _ when route.StartsWith(PluginPrefix, StringComparison.Ordinal) => route[PluginPrefix.Length..],
            _ when route.StartsWith(FeaturePrefix, StringComparison.Ordinal) => route[FeaturePrefix.Length..],
            _ => route,
        };

        if (candidate.Length == 0 || candidate.Contains(':', StringComparison.Ordinal) ||
            !BuiltInFeatureId.TryParseCanonical(candidate, out var id) ||
            !_catalog.TryGet(id, out _))
        {
            return NotFound(sourceRoute, correlationId);
        }

        var usesLegacyAlias = route.StartsWith(PluginPrefix, StringComparison.Ordinal) ||
                              route.StartsWith(FeaturePrefix, StringComparison.Ordinal);
        return BuiltInFeatureActivationResult.Success(
            id,
            sourceRoute,
            correlationId,
            usesLegacyAlias
                ? BuiltInFeatureActivationDisposition.Coalesced
                : BuiltInFeatureActivationDisposition.Activated);
    }

    public static string Emit(BuiltInFeatureId id) => id.Value;

    private static BuiltInFeatureActivationResult NotFound(string sourceRoute, string? correlationId) =>
        BuiltInFeatureActivationResult.Failure(
            BuiltInFeatureActivationErrorCode.NotFound,
            sourceRoute,
            correlationId: correlationId);
}
