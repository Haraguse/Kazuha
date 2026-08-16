namespace Luminalium.App.Services;

/// <summary>
/// Structured result returned by the storage / profile / backup services so the
/// view model can surface a user-readable outcome without throwing into the UI.
/// Mirrors the style of <c>ConfigurationSaveResult</c> / <c>ConfigurationLoadResult</c>.
/// </summary>
public sealed record StorageOperationResult(bool IsSuccess, string? ErrorMessage)
{
    public static StorageOperationResult Success() => new(true, null);

    public static StorageOperationResult Failure(string errorMessage) => new(false, errorMessage);
}