using System.Text.Json;
using Luminalium.App.Features;
using Xunit;

namespace Luminalium.Tests;

public sealed class BuiltInFeatureActivationTests
{
    [Fact]
    public void SuccessIncludesCanonicalIdentityAndCorrelationContext()
    {
        var result = BuiltInFeatureActivationResult.Success(
            BuiltInFeatureId.Timer,
            "plugin:timer",
            "correlation-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(BuiltInFeatureId.Timer, result.FeatureId);
        Assert.Equal("plugin:timer", result.SourceRoute);
        Assert.Equal("correlation-1", result.CorrelationId);
        Assert.Equal(BuiltInFeatureActivationDisposition.Activated, result.Disposition);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData(BuiltInFeatureActivationErrorCode.NotFound)]
    [InlineData(BuiltInFeatureActivationErrorCode.Unavailable)]
    [InlineData(BuiltInFeatureActivationErrorCode.InitializationFailed)]
    [InlineData(BuiltInFeatureActivationErrorCode.ShutdownInProgress)]
    [InlineData(BuiltInFeatureActivationErrorCode.UiDispatchFailed)]
    public void FailureCodesAreStableAndDoNotThrow(BuiltInFeatureActivationErrorCode code)
    {
        var result = BuiltInFeatureActivationResult.Failure(code, "unknown", correlationId: "correlation-2");

        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.Error!.Code);
        Assert.False(string.IsNullOrWhiteSpace(result.Error.Message));
        Assert.Equal("correlation-2", result.CorrelationId);
    }

    [Fact]
    public void CoalescedActivationIsObservableWithoutBecomingFailure()
    {
        var result = BuiltInFeatureActivationResult.Success(
            BuiltInFeatureId.StatusBar,
            disposition: BuiltInFeatureActivationDisposition.Coalesced);
        var diagnostic = BuiltInFeatureDiagnostic.FromResult(result);

        Assert.True(result.IsSuccess);
        Assert.Equal(BuiltInFeatureActivationDisposition.Coalesced, result.Disposition);
        Assert.Equal(BuiltInFeatureDiagnosticKind.ActivationCoalesced, diagnostic.Kind);
    }

    [Fact]
    public void DiagnosticSerializationExcludesSensitiveExceptionDetail()
    {
        const string sensitive = "password=hunter2 token=secret config=C:\\private";
        var result = BuiltInFeatureActivationResult.Failure(
            BuiltInFeatureActivationErrorCode.InitializationFailed,
            "timer",
            BuiltInFeatureId.Timer,
            "correlation-3",
            sensitive);

        var json = JsonSerializer.Serialize(BuiltInFeatureDiagnostic.FromResult(result));

        Assert.DoesNotContain("hunter2", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("private", json, StringComparison.Ordinal);
        Assert.Contains(nameof(BuiltInFeatureActivationErrorCode.InitializationFailed), json, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingCorrelationIdIsGenerated()
    {
        var result = BuiltInFeatureActivationResult.Success(BuiltInFeatureId.Board);

        Assert.Equal(32, result.CorrelationId.Length);
        Assert.True(Guid.TryParseExact(result.CorrelationId, "N", out _));
    }
}
