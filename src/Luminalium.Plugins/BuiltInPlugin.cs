namespace Luminalium.Plugins;

public sealed class BuiltInPlugin
{
    private readonly object _stateGate = new();

    public BuiltInPlugin(
        PluginMetadata metadata,
        IPluginCommand command,
        IPluginViewFactory viewFactory)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        ViewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
    }

    public PluginMetadata Metadata { get; }

    public IPluginCommand Command { get; }

    public IPluginViewFactory ViewFactory { get; }

    public bool IsActive { get; private set; }

    public async Task<PluginExecutionResult> ExecuteAsync(
        PluginContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        lock (_stateGate)
        {
            if (IsActive)
            {
                return PluginExecutionResult.Failure(new PluginExecutionError(
                    PluginExecutionErrorCode.ConcurrentActivation,
                    $"Built-in plugin '{Metadata.Id}' is already active.",
                    Metadata.Id));
            }

            IsActive = true;
        }

        try
        {
            var result = await Command.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                MarkInactive();
            }

            return result;
        }
        catch (OperationCanceledException exc)
        {
            MarkInactive();
            return PluginExecutionResult.Failure(new PluginExecutionError(
                PluginExecutionErrorCode.Cancelled,
                $"Built-in plugin '{Metadata.Id}' execution was cancelled.",
                Metadata.Id,
                exc.Message));
        }
        catch (Exception exc)
        {
            MarkInactive();
            return PluginExecutionResult.Failure(new PluginExecutionError(
                PluginExecutionErrorCode.ExecutionFailed,
                $"Built-in plugin '{Metadata.Id}' execution failed.",
                Metadata.Id,
                exc.Message));
        }
    }

    public async Task TerminateAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            IsActive = false;
        }

        await Command.TerminateAsync(cancellationToken).ConfigureAwait(false);
    }

    private void MarkInactive()
    {
        lock (_stateGate)
        {
            IsActive = false;
        }
    }
}
