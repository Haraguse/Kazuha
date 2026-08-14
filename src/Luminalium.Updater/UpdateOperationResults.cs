namespace Luminalium.Updater;

public enum UpdateErrorCode
{
    DevEnvironment,
    FeedUnavailable,
    ReleaseMalformed,
    AssetUnavailable,
    UpToDate,
    DownloadFailed,
    ChecksumMismatch,
    ValidationFailed,
    MutexHeld,
    ProcessExitTimeout,
    BackupFailed,
    ReplacementFailed,
    FileLocked,
    RollbackSucceeded,
    RollbackFailed,
}

public sealed record UpdateError(
    UpdateErrorCode Code,
    string Message,
    string? Detail = null,
    Exception? Exception = null);

public sealed record UpdateOperationResult(UpdateError? Error)
{
    public bool IsSuccess => Error is null;

    public static UpdateOperationResult Success() => new((UpdateError?)null);

    public static UpdateOperationResult Failure(UpdateError error) => new(error);
}

public sealed record UpdateOperationResult<T>(T? Value, UpdateError? Error)
{
    public bool IsSuccess => Error is null;
}

public static class UpdateOperation
{
    public static UpdateOperationResult<T> Success<T>(T value) => new(value, null);

    public static UpdateOperationResult<T> Failure<T>(UpdateError error, T? value = default) => new(value, error);
}
