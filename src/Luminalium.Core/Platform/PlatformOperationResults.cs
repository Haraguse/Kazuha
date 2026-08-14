namespace Luminalium.Core.Platform;

public enum PlatformOperationErrorCode
{
    Unavailable,
    AccessDenied,
    NotFound,
    Failed,
    UnsupportedVersion,
    AlreadyApplied,
}

public sealed record PlatformOperationError(
    PlatformOperationErrorCode Code,
    string Message,
    string? Detail = null,
    int? NativeErrorCode = null);

public sealed record PlatformCapability(
    bool IsSupported,
    string Name,
    PlatformOperationError? Reason = null,
    string? Detail = null);

public sealed record PlatformOperationWarning(
    PlatformOperationErrorCode Code,
    string Message,
    string? Detail = null);

public sealed record PlatformOperationResult(
    PlatformOperationError? Error,
    bool AlreadyApplied = false)
{
    public bool IsSuccess => Error is null;

    public static PlatformOperationResult Success() =>
        new((PlatformOperationError?)null);

    public static PlatformOperationResult AlreadyAppliedSuccess() =>
        new(null, AlreadyApplied: true);

    public static PlatformOperationResult Failure(PlatformOperationError error) =>
        new(error);
}

public sealed record PlatformOperationResult<T>(
    T Value,
    PlatformOperationError? Error,
    bool AlreadyApplied = false)
{
    public bool IsSuccess => Error is null;
}

public static class PlatformOperation
{
    public static PlatformOperationResult<T> Success<T>(T value) =>
        new(value, null);

    public static PlatformOperationResult<T> AlreadyApplied<T>(T value) =>
        new(value, null, AlreadyApplied: true);

    public static PlatformOperationResult<T> Failure<T>(PlatformOperationError error) =>
        new(default!, error);
}
