namespace Luminalium.App.Services;

public enum RetryCloseDialogResult
{
    Retried,
    Closed,
    Cancelled,
    OwnerNotReady,
    AlreadyOpen,
    Failed,
}

public sealed record RetryCloseDialogRequest(
    string Title,
    string Message,
    Func<Task<bool>> RetryAsync,
    Action Close);
