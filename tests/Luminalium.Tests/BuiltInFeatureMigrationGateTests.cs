using Luminalium.App.Features;
using Luminalium.App.Services;
using Luminalium.Core.Identity;
using Xunit;

namespace Luminalium.Tests;

public sealed class BuiltInFeatureMigrationGateTests
{
    [Fact]
    public void ReleaseMarkerDocumentsAliasesRemainForExactlyOneRelease()
    {
        Assert.Equal(1, CompatibilityReleaseMarker.CompatibilityReleaseCount);
        Assert.Equal("2.0.0", CompatibilityReleaseMarker.CompatibleUntilRelease);
        Assert.Contains("exactly 1 compatibility release", CompatibilityReleaseMarker.Statement, StringComparison.Ordinal);
        Assert.Contains("plugin:<id>", CompatibilityReleaseMarker.Statement, StringComparison.Ordinal);
    }

    [Fact]
    public void ParserRecordsLegacyAliasDiagnosticOnlyForAliasInputs()
    {
        var diagnostics = new BuiltInFeatureMigrationDiagnostics();
        var parser = new BuiltInFeatureRouteParser(diagnostics: diagnostics);

        parser.Parse("plugin:timer");
        parser.Parse("feature:logs");
        parser.Parse("timer");

        Assert.Equal(2, diagnostics.Count(BuiltInFeatureDiagnosticKind.LegacyAliasUsed));
    }

    [Fact]
    public void GatePassesAfterNativeMigrationWithOnlyDocumentedCompatibilitySurface()
    {
        var root = CreateFixtureRoot();
        try
        {
            // Compatibility exceptions present, but no production file references legacy surface
            // beyond the documented seams. BuiltInPluginCatalog.cs was removed in T14, so the
            // fixture contains no built-in plugin registration at all — only the files listed
            // in the compatibility exceptions.
            Write(root, "src/Luminalium.App/Services/BuiltInFeatureLegacyProjection.cs",
                "public sealed class BuiltInFeatureLegacyProjection { }");
            Write(root, "src/Luminalium.App/ViewModels/ShellViewModel.cs",
                "private readonly BuiltInFeatureLegacyProjection _legacyProjection = new();");
            Write(root, "src/Luminalium.App/Features/BuiltInFeatureRouteParser.cs",
                "private const string PluginPrefix = \"plugin:\";");
            Write(root, "src/Luminalium.App/ViewModels/OnboardingViewModel.cs",
                ": base(\"plugin:onboarding\", \"Plugin.onboarding.Name\", localization)");
            Write(root, "src/Luminalium.App/ViewModels/LogsViewModel.cs",
                ": base(\"plugin:logs\", \"Plugin.logs.Name\", localization)");
            Write(root, "src/Luminalium.App/ViewModels/ExtensionPageViewModel.cs",
                ": base($\"plugin:{extension.Id}\", \"PluginPage.Title\", localization)");
            Write(root, "src/Luminalium.App/Services/NativeBuiltInFeatureHostFactory.cs",
                "new BoardWindow(), new TimerWindow(), new StatusBarWindow(), new AppLauncherWindow(), new SpotlightWindow()");
            Write(root, "src/Luminalium.App/Views/NativeFeatureView.axaml.cs",
                "public sealed class NativeFeatureView { }");
            Write(root, "tests/Luminalium.Tests/LegacyConsumerTests.cs",
                "var plugin = new BuiltInPlugin(...); new BoardWindow(); \"plugin:timer\"; BuiltInFeatureLegacyProjection p; PlaceholderPluginViewFactory f;");

            var report = new NativeFeatureMigrationGate().Evaluate(root);

            Assert.True(report.Passed);
            Assert.Empty(report.Findings);
            Assert.NotEmpty(report.CompatibilityExceptions);
            Assert.Contains("exactly", report.ReleaseStatement, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GateBlocksPrematureRemovalReportingFileCategoryAndRemediation()
    {
        var root = CreateFixtureRoot();
        try
        {
            Write(root, "src/Luminalium.App/Views/LegacyConsumer.cs",
                """
                public sealed class LegacyConsumer
                {
                    public void Run()
                    {
                        var window = new BoardWindow();
                        var plugin = new BuiltInPlugin(meta, new PlaceholderPluginCommand(id), new PlaceholderPluginViewFactory("x"));
                        var projection = new BuiltInFeatureLegacyProjection();
                        var route = "plugin:legacy";
                    }
                }
                """);

            var report = new NativeFeatureMigrationGate().Evaluate(root);

            Assert.False(report.Passed);
            var categories = report.Findings.Select(finding => finding.Category).ToHashSet(StringComparer.Ordinal);
            Assert.Contains(NativeFeatureMigrationGate.CategoryDirectNativeConstruction, categories);
            Assert.Contains(NativeFeatureMigrationGate.CategoryBuiltInPluginRegistration, categories);
            Assert.Contains(NativeFeatureMigrationGate.CategoryPlaceholderInvocation, categories);
            Assert.Contains(NativeFeatureMigrationGate.CategoryLegacyProjectionConsumer, categories);
            Assert.Contains(NativeFeatureMigrationGate.CategoryLegacyAliasEmission, categories);
            Assert.Contains(
                report.Findings,
                finding => finding.File.EndsWith("LegacyConsumer.cs", StringComparison.Ordinal) &&
                           !string.IsNullOrWhiteSpace(finding.Remediation));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GatePassesAgainstActualRepositoryAfterNativeMigration()
    {
        var root = ResolveRepositoryRoot();
        var report = new NativeFeatureMigrationGate().Evaluate(root);

        Assert.True(report.Passed);
        Assert.Empty(report.Findings);
    }

    [Fact]
    public void GateBlocksWhenRuntimeLegacyCountersAreNonZero()
    {
        var root = CreateFixtureRoot();
        try
        {
            var diagnostics = new BuiltInFeatureMigrationDiagnostics();
            diagnostics.Record(BuiltInFeatureDiagnosticKind.LegacyProjectionUsed);
            diagnostics.Record(BuiltInFeatureDiagnosticKind.PlaceholderFactoryUsed);

            var report = new NativeFeatureMigrationGate(diagnostics).Evaluate(root);

            Assert.False(report.Passed);
            var runtimeFindings = report.Findings
                .Where(finding => finding.Category == NativeFeatureMigrationGate.CategoryRuntimeLegacyUsage)
                .ToArray();
            Assert.Equal(2, runtimeFindings.Length);
            Assert.Contains(runtimeFindings, finding => finding.Remediation.Contains(nameof(BuiltInFeatureDiagnosticKind.LegacyProjectionUsed), StringComparison.Ordinal));
            Assert.Contains(runtimeFindings, finding => finding.Remediation.Contains(nameof(BuiltInFeatureDiagnosticKind.PlaceholderFactoryUsed), StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GateReportRenderIncludesReleaseStatementAndFindings()
    {
        var root = CreateFixtureRoot();
        try
        {
            Write(root, "src/Luminalium.App/Views/LegacyConsumer.cs", "new BoardWindow();");

            var rendered = new NativeFeatureMigrationGate().Evaluate(root).Render();

            Assert.Contains("Native built-in feature migration gate", rendered, StringComparison.Ordinal);
            Assert.Contains("Result: FAIL", rendered, StringComparison.Ordinal);
            Assert.Contains("Compatibility release marker", rendered, StringComparison.Ordinal);
            Assert.Contains(NativeFeatureMigrationGate.CategoryDirectNativeConstruction, rendered, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EmitRemovalGateEvidenceReports()
    {
        var repositoryRoot = ResolveRepositoryRoot();

        // QA scenario 1: gate passes after native migration.
        var passReport = new NativeFeatureMigrationGate().Evaluate(repositoryRoot);
        WriteEvidence(repositoryRoot, "removal-gate.txt", passReport.Render());
        Assert.True(passReport.Passed);

        // QA scenario 2: gate blocks premature removal with an intentional violation fixture.
        var fixture = CreateFixtureRoot();
        try
        {
            Write(fixture, "src/Luminalium.App/Views/LegacyConsumer.cs",
                "var window = new BoardWindow();\nvar plugin = new BuiltInPlugin(m, new PlaceholderPluginCommand(id), new PlaceholderPluginViewFactory(\"x\"));");
            var failReport = new NativeFeatureMigrationGate().Evaluate(fixture);
            WriteEvidence(repositoryRoot, "removal-gate-error.txt", failReport.Render());

            Assert.False(failReport.Passed);
            Assert.NotEmpty(failReport.Findings);
            Assert.Contains(failReport.Findings, finding => finding.File.EndsWith("LegacyConsumer.cs", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(fixture, recursive: true);
        }
    }

    private static void WriteEvidence(string repositoryRoot, string fileName, string content)
    {
        var path = Path.Combine(repositoryRoot, "artifacts", "feature-migration", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, NativeFeatureMigrationGate.RepoRootFileName)))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate the repository root for the gate test.");
    }

    private static string CreateFixtureRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "lumi-gate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, NativeFeatureMigrationGate.RepoRootFileName), string.Empty);
        return root;
    }

    private static void Write(string root, string relativePath, string content)
    {
        var full = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }
}
