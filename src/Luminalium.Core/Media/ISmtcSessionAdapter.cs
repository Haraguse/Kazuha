using Luminalium.Core.Platform;

namespace Luminalium.Core.Media;

public interface ISmtcSessionAdapter
{
    Task<PlatformOperationResult<SmtcSessionSnapshot>> TryGetSnapshotAsync(
        CancellationToken cancellationToken = default);
}
