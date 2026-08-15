using Luminalium.App.Features;
using Xunit;

namespace Luminalium.Tests;

public sealed class BuiltInFeatureRouteTests
{
    private static readonly string[] CanonicalIds =
    [
        "settings", "onboarding", "logs", "board", "timer", "spotlight", "app_launcher", "status_bar",
    ];

    [Theory]
    [MemberData(nameof(AllCanonicalRoutes))]
    public void BareAndCompatibilityRoutesNormalizeToCanonicalIdentity(string route, string expectedId)
    {
        var result = new BuiltInFeatureRouteParser().Parse(route, "route-correlation");

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedId, result.FeatureId!.Value.Value);
        Assert.Equal("route-correlation", result.CorrelationId);
        Assert.Equal(route, result.SourceRoute);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("plugin:")]
    [InlineData("plugin:unknown")]
    [InlineData("feature:unknown")]
    [InlineData("external.test.plugin")]
    [InlineData("plugin:external.test.plugin")]
    [InlineData("plugin:timer:extra")]
    public void MalformedUnknownAndExternalRoutesReturnNotFound(string? route)
    {
        var result = new BuiltInFeatureRouteParser().Parse(route);

        Assert.False(result.IsSuccess);
        Assert.Equal(BuiltInFeatureActivationErrorCode.NotFound, result.Error!.Code);
        Assert.Equal(route ?? string.Empty, result.SourceRoute);
    }

    [Fact]
    public void ParserDoesNotTrimCanonicalIdsAndEmitterNeverAddsLegacyPrefix()
    {
        var parser = new BuiltInFeatureRouteParser();
        var result = parser.Parse(" timer");

        Assert.False(result.IsSuccess);
        Assert.Equal("timer", BuiltInFeatureRouteParser.Emit(BuiltInFeatureId.Timer));
        Assert.DoesNotContain("plugin:", BuiltInFeatureRouteParser.Emit(BuiltInFeatureId.Timer), StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> AllCanonicalRoutes()
    {
        foreach (var id in CanonicalIds)
        {
            yield return [id, id];
            yield return [$"plugin:{id}", id];
            yield return [$"feature:{id}", id];
        }
    }
}
