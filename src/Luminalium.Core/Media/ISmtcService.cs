using Luminalium.Core.Platform;

namespace Luminalium.Core.Media;

public interface ISmtcService
{
    Task<PlatformOperationResult<SmtcSessionSnapshot>> TryGetSnapshotAsync(
        CancellationToken cancellationToken = default);

    Task<PlatformOperationResult<SmtcSessionSnapshot>> GetCurrentSessionAsync(
        CancellationToken cancellationToken = default);
}
