using System.Collections.Concurrent;

namespace Luminalium.App.Services;

public sealed class BuiltInFeatureMigrationDiagnostics
{
    private readonly ConcurrentDictionary<Features.BuiltInFeatureDiagnosticKind, int> _counts = new();

    public void Record(Features.BuiltInFeatureDiagnosticKind kind) =>
        _counts.AddOrUpdate(kind, 1, static (_, count) => count + 1);

    public IReadOnlyDictionary<Features.BuiltInFeatureDiagnosticKind, int> Snapshot() =>
        new Dictionary<Features.BuiltInFeatureDiagnosticKind, int>(_counts);

    public int Count(Features.BuiltInFeatureDiagnosticKind kind) =>
        _counts.TryGetValue(kind, out var count) ? count : 0;
}
