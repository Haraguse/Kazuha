using Luminalium.Core.Platform;

namespace Luminalium.Core.Media;

/// <summary>
/// In-process SMTC service wrapper. Each read forwards to the supplied adapter;
/// there is no helper executable or JSON-line worker to start or reuse.
/// </summary>
public sealed class SmtcSessionService : ISmtcService
{
    private readonly ISmtcSessionAdapter _adapter;

    public SmtcSessionService(ISmtcSessionAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        _adapter = adapter;
    }

    /// <summary>
    /// Reads a session snapshot by invoking the adapter once. Repeated calls are
    /// independent in-process adapter calls and cannot spawn a second helper process.
    /// </summary>
    public Task<PlatformOperationResult<SmtcSessionSnapshot>> TryGetSnapshotAsync(
        CancellationToken cancellationToken = default) =>
        _adapter.TryGetSnapshotAsync(cancellationToken);

    /// <summary>
    /// Reads the current best SMTC session. This is an alias for
    /// <see cref="TryGetSnapshotAsync"/> because ranking is owned by the adapter.
    /// </summary>
    public Task<PlatformOperationResult<SmtcSessionSnapshot>> GetCurrentSessionAsync(
        CancellationToken cancellationToken = default) =>
        TryGetSnapshotAsync(cancellationToken);
}
