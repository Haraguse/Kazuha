namespace Luminalium.Plugins;

public enum PluginExecutionErrorCode
{
    ExecutionFailed,
    Cancelled,
    NotSupported,
    ConcurrentActivation,
}

public sealed record PluginExecutionError(
    PluginExecutionErrorCode Code,
    string Message,
    string? PluginId = null,
    string? Detail = null);

public sealed record PluginExecutionResult(PluginExecutionError? Error)
{
    public bool IsSuccess => Error is null;

    public static PluginExecutionResult Success() =>
        new((PluginExecutionError?)null);

    public static PluginExecutionResult Failure(PluginExecutionError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PluginExecutionResult(error);
    }
}
