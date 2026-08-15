namespace Luminalium.App.Features;

public enum BuiltInFeatureActivationErrorCode
{
    NotFound,
    Unavailable,
    InitializationFailed,
    ShutdownInProgress,
    UiDispatchFailed,
}

public enum BuiltInFeatureActivationDisposition
{
    Activated,
    Coalesced,
}

public sealed record BuiltInFeatureActivationError(
    BuiltInFeatureActivationErrorCode Code,
    string Message,
    string? Detail = null);

public sealed record BuiltInFeatureActivationResult
{
    private BuiltInFeatureActivationResult(
        bool isSuccess,
        BuiltInFeatureId? featureId,
        string sourceRoute,
        string correlationId,
        BuiltInFeatureActivationDisposition? disposition,
        BuiltInFeatureActivationError? error)
    {
        IsSuccess = isSuccess;
        FeatureId = featureId;
        SourceRoute = sourceRoute;
        CorrelationId = correlationId;
        Disposition = disposition;
        Error = error;
    }

    public bool IsSuccess { get; }
    public BuiltInFeatureId? FeatureId { get; }
    public string SourceRoute { get; }
    public string CorrelationId { get; }
    public BuiltInFeatureActivationDisposition? Disposition { get; }
    public BuiltInFeatureActivationError? Error { get; }

    public static BuiltInFeatureActivationResult Success(
        BuiltInFeatureId featureId,
        string? sourceRoute = null,
        string? correlationId = null,
        BuiltInFeatureActivationDisposition disposition = BuiltInFeatureActivationDisposition.Activated) =>
        new(
            true,
            featureId,
            sourceRoute ?? featureId.Value,
            NormalizeCorrelationId(correlationId),
            disposition,
            null);

    public static BuiltInFeatureActivationResult Failure(
        BuiltInFeatureActivationErrorCode code,
        string? sourceRoute,
        BuiltInFeatureId? featureId = null,
        string? correlationId = null,
        string? detail = null) =>
        new(
            false,
            featureId,
            sourceRoute ?? string.Empty,
            NormalizeCorrelationId(correlationId),
            null,
            new BuiltInFeatureActivationError(code, MessageFor(code), detail));

    private static string NormalizeCorrelationId(string? correlationId) =>
        string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId;

    private static string MessageFor(BuiltInFeatureActivationErrorCode code) => code switch
    {
        BuiltInFeatureActivationErrorCode.NotFound => "The built-in feature was not found.",
        BuiltInFeatureActivationErrorCode.Unavailable => "The built-in feature is unavailable.",
        BuiltInFeatureActivationErrorCode.InitializationFailed => "The built-in feature could not be initialized.",
        BuiltInFeatureActivationErrorCode.ShutdownInProgress => "Built-in feature activation is unavailable during shutdown.",
        BuiltInFeatureActivationErrorCode.UiDispatchFailed => "The built-in feature could not be dispatched to the UI thread.",
        _ => "The built-in feature could not be activated.",
    };
}
