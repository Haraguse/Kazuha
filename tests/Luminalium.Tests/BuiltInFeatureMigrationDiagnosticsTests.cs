using System.Text.Json;
using Luminalium.App.Features;
using Luminalium.App.Services;
using Xunit;

namespace Luminalium.Tests;

public sealed class BuiltInFeatureMigrationDiagnosticsTests
{
    [Fact]
    public void RecordIncrementsCountDeterministically()
    {
        var diagnostics = new BuiltInFeatureMigrationDiagnostics();

        diagnostics.Record(BuiltInFeatureDiagnosticKind.LegacyAliasUsed);
        diagnostics.Record(BuiltInFeatureDiagnosticKind.LegacyAliasUsed);
        diagnostics.Record(BuiltInFeatureDiagnosticKind.LegacyProjectionUsed);

        Assert.Equal(2, diagnostics.Count(BuiltInFeatureDiagnosticKind.LegacyAliasUsed));
        Assert.Equal(1, diagnostics.Count(BuiltInFeatureDiagnosticKind.LegacyProjectionUsed));
        Assert.Equal(0, diagnostics.Count(BuiltInFeatureDiagnosticKind.PlaceholderFactoryUsed));
    }

    [Fact]
    public void SnapshotIsIndependentCopy()
    {
        var diagnostics = new BuiltInFeatureMigrationDiagnostics();
        diagnostics.Record(BuiltInFeatureDiagnosticKind.DuplicateRegistration);

        var snapshot = diagnostics.Snapshot();
        diagnostics.Record(BuiltInFeatureDiagnosticKind.DuplicateRegistration);

        Assert.Equal(1, snapshot[BuiltInFeatureDiagnosticKind.DuplicateRegistration]);
        Assert.Equal(2, diagnostics.Count(BuiltInFeatureDiagnosticKind.DuplicateRegistration));
    }

    [Fact]
    public void DiagnosticCarriesCanonicalIdAndCorrelationContext()
    {
        var result = BuiltInFeatureActivationResult.Failure(
            BuiltInFeatureActivationErrorCode.NotFound,
            "plugin:unknown",
            BuiltInFeatureId.Timer,
            "correlation-4");

        var diagnostic = BuiltInFeatureDiagnostic.FromResult(result);

        Assert.Equal(BuiltInFeatureDiagnosticKind.ActivationFailed, diagnostic.Kind);
        Assert.Equal("timer", diagnostic.CanonicalId);
        Assert.Equal("plugin:unknown", diagnostic.SourceRoute);
        Assert.Equal("correlation-4", diagnostic.CorrelationId);
        Assert.Equal(nameof(BuiltInFeatureActivationErrorCode.NotFound), diagnostic.FailureCategory);
    }

    [Fact]
    public void DiagnosticSerializationExcludesSensitiveValues()
    {
        const string sensitive = "token=abc123 password=topsecret";
        var result = BuiltInFeatureActivationResult.Failure(
            BuiltInFeatureActivationErrorCode.InitializationFailed,
            "timer",
            BuiltInFeatureId.Timer,
            "correlation-5",
            sensitive);

        var json = JsonSerializer.Serialize(BuiltInFeatureDiagnostic.FromResult(result));

        Assert.DoesNotContain("abc123", json, StringComparison.Ordinal);
        Assert.DoesNotContain("topsecret", json, StringComparison.Ordinal);
        Assert.Contains(nameof(BuiltInFeatureActivationErrorCode.InitializationFailed), json, StringComparison.Ordinal);
    }
}
