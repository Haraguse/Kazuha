using System.Globalization;
using System.Text;
using Luminalium.App.Features;
using Luminalium.Core.Identity;

namespace Luminalium.App.Services;

/// <summary>
/// A single deterministic gate blocking premature removal of the legacy built-in surface (T13).
/// The gate combines a repository-wide static reference scan of production source with runtime
/// migration diagnostic counters, and reports every remaining legacy reference as an explicit
/// compatibility exception. Tests are intentionally excluded from static checks so the report
/// distinguishes intentional compatibility tests from production usage.
/// </summary>
public sealed class NativeFeatureMigrationGate
{
    /// <summary>File at the repository root used to locate the checkout when none is provided.</summary>
    public const string RepoRootFileName = "Luminalium.sln";

    public const string CategoryBuiltInPluginRegistration = "builtin-plugin-registration";
    public const string CategoryPlaceholderInvocation = "placeholder-invocation";
    public const string CategoryDirectNativeConstruction = "direct-native-construction";
    public const string CategoryLegacyProjectionConsumer = "legacy-projection-consumer";
    public const string CategoryLegacyAliasEmission = "legacy-alias-emission";
    public const string CategoryRuntimeLegacyUsage = "runtime-legacy-usage";

    /// <summary>Native window features that must be constructed only through the feature host.</summary>
    private static readonly string[] NativeWindowTypes =
        ["AppLauncherWindow", "BoardWindow", "StatusBarWindow", "TimerWindow", "SpotlightWindow"];

    private const string ProjectionPath = "src/Luminalium.App/Services/BuiltInFeatureLegacyProjection.cs";
    private const string ShellViewModelPath = "src/Luminalium.App/ViewModels/ShellViewModel.cs";
    private const string ParserPath = "src/Luminalium.App/Features/BuiltInFeatureRouteParser.cs";
    private const string NativeHostFactoryPath = "src/Luminalium.App/Services/NativeBuiltInFeatureHostFactory.cs";

    /// <summary>
    /// This gate's own source references every legacy pattern as search text; it is excluded from
    /// the static scan so it cannot report itself.
    /// </summary>
    private const string GatePath = "src/Luminalium.App/Services/NativeFeatureMigrationGate.cs";

    private static readonly string[] LegacyAliasExceptionPaths =
    [
        ParserPath,
        "src/Luminalium.App/ViewModels/OnboardingViewModel.cs",
        "src/Luminalium.App/ViewModels/LogsViewModel.cs",
        "src/Luminalium.App/ViewModels/ExtensionPageViewModel.cs",
    ];

    /// <summary>Explicit compatibility exceptions: the only production sites allowed to touch legacy surface.</summary>
    private static readonly string[] CompatibilityExceptions =
    [
        $"{ProjectionPath} (one-way native catalog -> legacy projection adapter)",
        $"{ShellViewModelPath} (Plugins/LegacyPlugins overview compatibility surface for one release)",
        $"{ParserPath} (input-boundary `plugin:`/`feature:` alias normalization only)",
        "src/Luminalium.App/ViewModels/OnboardingViewModel.cs (legacy carrier id `plugin:onboarding` on the input boundary)",
        "src/Luminalium.App/ViewModels/LogsViewModel.cs (legacy carrier id `plugin:logs` on the input boundary)",
        "src/Luminalium.App/ViewModels/ExtensionPageViewModel.cs (genuine external extension carrier `plugin:{id}`)",
        $"{NativeHostFactoryPath} (sole direct native construction site, behind the feature host)",
        "tests/Luminalium.Tests/** (intentional compatibility tests)",
    ];

    /// <summary>Runtime counters that must be zero in native user flows.</summary>
    private static readonly BuiltInFeatureDiagnosticKind[] BlockingRuntimeKinds =
    [
        BuiltInFeatureDiagnosticKind.LegacyAliasUsed,
        BuiltInFeatureDiagnosticKind.LegacyProjectionUsed,
        BuiltInFeatureDiagnosticKind.PlaceholderFactoryUsed,
        BuiltInFeatureDiagnosticKind.DirectNativeConstruction,
        BuiltInFeatureDiagnosticKind.DuplicateRegistration,
    ];

    private readonly BuiltInFeatureMigrationDiagnostics _diagnostics;

    public NativeFeatureMigrationGate(BuiltInFeatureMigrationDiagnostics? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new BuiltInFeatureMigrationDiagnostics();
    }

    public NativeFeatureMigrationGateReport Evaluate(string? repositoryRoot = null)
    {
        var root = ResolveRepositoryRoot(repositoryRoot);
        var findings = new List<NativeFeatureMigrationGateFinding>();

        foreach (var (path, content) in EnumerateSourceFiles(root))
        {
            ScanSource(root, path, content, findings);
        }

        findings.AddRange(ScanRuntimeCounters());

        return new NativeFeatureMigrationGateReport(
            findings.Count == 0,
            findings,
            CompatibilityExceptions,
            _diagnostics.Snapshot(),
            CompatibilityReleaseMarker.Statement);
    }

    private static void ScanSource(string root, string path, string content, List<NativeFeatureMigrationGateFinding> findings)
    {
        var relative = Normalize(Path.GetRelativePath(root, path));
        if (IsTestFile(relative) || relative == GatePath)
        {
            return;
        }

        if (content.Contains("new BuiltInPlugin(", StringComparison.Ordinal))
        {
            findings.Add(new NativeFeatureMigrationGateFinding(
                relative,
                CategoryBuiltInPluginRegistration,
                "No built-in feature may be registered as a plugin after T14; only genuine external plugins reach the registry."));
        }

        if (content.Contains("PlaceholderPluginCommand", StringComparison.Ordinal) ||
            content.Contains("PlaceholderPluginViewFactory", StringComparison.Ordinal) ||
            content.Contains("NativeSurfaceViewFactory", StringComparison.Ordinal))
        {
            findings.Add(new NativeFeatureMigrationGateFinding(
                relative,
                CategoryPlaceholderInvocation,
                "Placeholder command/view factories must not be invoked in production; remove the placeholder path (T14)."));
        }

        foreach (var windowType in NativeWindowTypes)
        {
            if (content.Contains($"new {windowType}(", StringComparison.Ordinal) && relative != NativeHostFactoryPath)
            {
                findings.Add(new NativeFeatureMigrationGateFinding(
                    relative,
                    CategoryDirectNativeConstruction,
                    $"Construct '{windowType}' only inside NativeBuiltInFeatureHostFactory; route native windows through the feature host."));
            }
        }

        if (content.Contains("BuiltInFeatureLegacyProjection", StringComparison.Ordinal) &&
            relative != ProjectionPath && relative != ShellViewModelPath)
        {
            findings.Add(new NativeFeatureMigrationGateFinding(
                relative,
                CategoryLegacyProjectionConsumer,
                "The legacy projection is a one-release compatibility adapter; new code must consume native feature descriptors only."));
        }

        if (content.Contains("\"plugin:", StringComparison.Ordinal) &&
            !LegacyAliasExceptionPaths.Contains(relative, StringComparer.Ordinal))
        {
            findings.Add(new NativeFeatureMigrationGateFinding(
                relative,
                CategoryLegacyAliasEmission,
                "Do not emit `plugin:` aliases from new internal state; aliases are input-boundary only (BuiltInFeatureRouteParser) plus documented carrier ids."));
        }
    }

    private List<NativeFeatureMigrationGateFinding> ScanRuntimeCounters()
    {
        var findings = new List<NativeFeatureMigrationGateFinding>();
        foreach (var kind in BlockingRuntimeKinds)
        {
            if (_diagnostics.Count(kind) != 0)
            {
                findings.Add(new NativeFeatureMigrationGateFinding(
                    "<runtime>",
                    CategoryRuntimeLegacyUsage,
                    $"Runtime counter '{kind}' is non-zero in native user flows; legacy usage must be zero before removal."));
            }
        }

        return findings;
    }

    private static bool IsTestFile(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment.Equals("tests", StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static string ResolveRepositoryRoot(string? provided)
    {
        if (!string.IsNullOrWhiteSpace(provided))
        {
            return Path.GetFullPath(provided);
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, RepoRootFileName)))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException(
            $"Unable to locate repository root (missing {RepoRootFileName}). Provide an explicit repositoryRoot.");
    }

    private static IEnumerable<(string Path, string Content)> EnumerateSourceFiles(string root)
    {
        foreach (var directory in new[] { Path.Combine(root, "src"), Path.Combine(root, "tests") })
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Normalize(Path.GetRelativePath(root, file));
                if (relative.Contains("/bin/") || relative.Contains("/obj/"))
                {
                    continue;
                }

                yield return (file, File.ReadAllText(file));
            }
        }
    }
}

public sealed record NativeFeatureMigrationGateFinding(
    string File,
    string Category,
    string Remediation);

public sealed record NativeFeatureMigrationGateReport(
    bool Passed,
    IReadOnlyList<NativeFeatureMigrationGateFinding> Findings,
    IReadOnlyList<string> CompatibilityExceptions,
    IReadOnlyDictionary<BuiltInFeatureDiagnosticKind, int> RuntimeCounters,
    string ReleaseStatement)
{
    public string Render()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Native built-in feature migration gate");
        builder.Append("Result: ").AppendLine(Passed ? "PASS" : "FAIL");
        builder.AppendLine();
        builder.AppendLine("Compatibility release marker:");
        builder.Append("  ").AppendLine(ReleaseStatement);
        builder.AppendLine();
        builder.AppendLine("Compatibility exceptions (explicit):");
        foreach (var exception in CompatibilityExceptions)
        {
            builder.Append("  - ").AppendLine(exception);
        }

        builder.AppendLine();
        builder.AppendLine("Runtime diagnostic counters:");
        foreach (var kind in RuntimeCounters.Keys.OrderBy(kind => kind.ToString()))
        {
            builder.Append("  ").Append(kind.ToString());
            builder.Append(" = ");
            builder.AppendLine(RuntimeCounters[kind].ToString(CultureInfo.InvariantCulture));
        }

        builder.AppendLine();
        if (Findings.Count == 0)
        {
            builder.AppendLine("Static reference scan: no violations outside the explicit compatibility exceptions.");
            return builder.ToString();
        }

        builder.AppendLine("Static reference scan findings:");
        foreach (var finding in Findings)
        {
            builder.Append("  [").Append(finding.Category).Append("] ").Append(finding.File);
            builder.Append(": ").AppendLine(finding.Remediation);
        }

        return builder.ToString();
    }
}
