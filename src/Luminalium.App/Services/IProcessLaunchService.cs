using Luminalium.Core.Platform;

namespace Luminalium.App.Services;

/// <summary>
/// Injectable process-launch surface for the app launcher plugin. The real
/// implementation validates the target path BEFORE starting any process and
/// returns a typed result instead of throwing.
/// </summary>
public interface IProcessLaunchService
{
    PlatformOperationResult Launch(string path);
}

/// <summary>
/// Real launcher used by the app shell. Missing or empty paths fail with a
/// typed NotFound result and NO process is ever started. Invalid paths must
/// not reach <see cref="System.Diagnostics.Process.Start"/>.
/// </summary>
public sealed class ProcessLaunchService : IProcessLaunchService
{
    public PlatformOperationResult Launch(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.NotFound,
                "Launch path is empty."));
        }

        if (!File.Exists(path))
        {
            return PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.NotFound,
                $"File not found: {path}"));
        }

        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
            return process is null
                ? PlatformOperationResult.Failure(new PlatformOperationError(
                    PlatformOperationErrorCode.Failed,
                    "The operating system did not start the requested application."))
                : PlatformOperationResult.Success();
        }
        catch (Exception exception)
        {
            return PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.Failed,
                exception.Message));
        }
    }
}
