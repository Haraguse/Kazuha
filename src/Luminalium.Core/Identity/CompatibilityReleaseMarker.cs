namespace Luminalium.Core.Identity;

/// <summary>
/// Versioned marker documenting how long the native built-in feature migration keeps the legacy
/// <c>plugin:&lt;id&gt;</c> input alias and its compatibility surface.
/// </summary>
/// <remarks>
/// The compatibility surface (alias parsing, legacy projection, placeholders) is retained for exactly
/// one compatibility release after the native migration lands. This marker is the single source of
/// truth for that boundary: T14 is only permitted to remove the legacy surface after a green gate at
/// this marker's release, and the gate documents the exact release window in its report.
/// </remarks>
public static class CompatibilityReleaseMarker
{
    /// <summary>
    /// Product version in which the native built-in feature migration ships (aliases begin to be
    /// deprecated rather than removed).
    /// </summary>
    public const string MigrationRelease = "1.0.0";

    /// <summary>
    /// The single compatibility release window. <c>plugin:&lt;id&gt;</c> input aliases remain valid for
    /// exactly one release after <see cref="MigrationRelease"/> and then must be removed.
    /// </summary>
    public const int CompatibilityReleaseCount = 1;

    /// <summary>
    /// Canonical release boundary: the product version after which the compatibility surface is no
    /// longer supported and removal (T14) is authorized.
    /// </summary>
    public const string CompatibleUntilRelease = "2.0.0";

    /// <summary>
    /// Human-readable statement surfaced in the gate report, documenting that aliases remain for
    /// exactly one release.
    /// </summary>
    public static string Statement =>
        $"The native built-in feature migration ships in release {MigrationRelease}. " +
        $"The legacy `plugin:<id>` input alias and its compatibility surface remain valid for exactly " +
        $"{CompatibilityReleaseCount} compatibility release ({CompatibilityReleaseCount} release after " +
        $"{MigrationRelease}, i.e. until release {CompatibleUntilRelease}), after which they must be removed.";
}
