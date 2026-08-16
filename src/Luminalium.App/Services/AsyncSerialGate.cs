namespace Luminalium.App.Services;

internal sealed class AsyncSerialGate
{
    private readonly object _sync = new();
    private Task _tail = Task.CompletedTask;

    public Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        Task prior;
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            prior = _tail;
            _tail = completion.Task;
        }

        return RunAfterAsync(prior, completion, operation);
    }

    private static async Task<T> RunAfterAsync<T>(
        Task prior,
        TaskCompletionSource<object?> completion,
        Func<Task<T>> operation)
    {
        try
        {
            await prior.ConfigureAwait(true);
            return await operation().ConfigureAwait(true);
        }
        finally
        {
            completion.TrySetResult(null);
        }
    }
}
