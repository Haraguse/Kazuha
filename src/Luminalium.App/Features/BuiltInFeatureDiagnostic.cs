namespace Luminalium.App.Features;

public enum BuiltInFeatureDiagnosticKind
{
    ActivationSucceeded,
    ActivationFailed,
    ActivationCoalesced,
    LegacyAliasUsed,
    LegacyProjectionUsed,
    PlaceholderFactoryUsed,
    DirectNativeConstruction,
    DuplicateRegistration,
}

public sealed record BuiltInFeatureDiagnostic(
    BuiltInFeatureDiagnosticKind Kind,
    string? CanonicalId,
    string SourceRoute,
    string CorrelationId,
    string? FailureCategory)
{
    public static BuiltInFeatureDiagnostic FromResult(BuiltInFeatureActivationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var kind = result.IsSuccess
            ? result.Disposition == BuiltInFeatureActivationDisposition.Coalesced
                ? BuiltInFeatureDiagnosticKind.ActivationCoalesced
                : BuiltInFeatureDiagnosticKind.ActivationSucceeded
            : BuiltInFeatureDiagnosticKind.ActivationFailed;

        return new BuiltInFeatureDiagnostic(
            kind,
            result.FeatureId?.Value,
            result.SourceRoute,
            result.CorrelationId,
            result.Error?.Code.ToString());
    }
}
