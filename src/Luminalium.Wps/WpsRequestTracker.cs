using System.Collections.Concurrent;
using Luminalium.Core.Platform;

namespace Luminalium.Wps;

public sealed class WpsRequestTracker
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<WpsMessage>> _pending = new(StringComparer.Ordinal);

    public async Task<PlatformOperationResult<WpsMessage>> TrackAsync(
        string messageId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return Failure<WpsMessage>("WPS request message_id is required", messageId);
        }

        var completion = new TaskCompletionSource<WpsMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(messageId, completion))
        {
            return Failure<WpsMessage>($"WPS request is already tracked: {messageId}", messageId);
        }

        try
        {
            var completed = await Task.WhenAny(completion.Task, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);
            if (completed == completion.Task)
            {
                return PlatformOperation.Success(await completion.Task.ConfigureAwait(false));
            }

            _pending.TryRemove(messageId, out _);
            return Failure<WpsMessage>($"Timed out waiting for WPS response: {messageId}", messageId);
        }
        catch (OperationCanceledException)
        {
            _pending.TryRemove(messageId, out _);
            return Failure<WpsMessage>($"Cancelled while waiting for WPS response: {messageId}", messageId);
        }
    }

    public PlatformOperationResult Complete(string messageId, WpsMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_pending.TryRemove(messageId, out var completion))
        {
            return PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.NotFound,
                $"No tracked WPS request exists for message_id: {messageId}",
                messageId));
        }

        completion.TrySetResult(message);
        return PlatformOperationResult.Success();
    }

    private static PlatformOperationResult<T> Failure<T>(string message, string? detail = null) =>
        PlatformOperation.Failure<T>(new PlatformOperationError(
            PlatformOperationErrorCode.Failed,
            message,
            detail));
}
