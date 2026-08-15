namespace Luminalium.Plugins;

public abstract record PluginRegistryError(string Message, string? PluginId = null);

public sealed record DuplicateRegistrationError(string DuplicateId)
    : PluginRegistryError($"Built-in plugin '{DuplicateId}' is already registered.", DuplicateId);

public sealed record PluginRegistrationValidationError(string ValidationMessage, string? InvalidPluginId = null)
    : PluginRegistryError(ValidationMessage, InvalidPluginId);

/// <summary>
/// An external plugin ID collided with an ID reserved by the host application
/// (for Luminalium, a canonical native built-in feature). The registry stays
/// generic; the host application supplies the reserved-by policy.
/// </summary>
public sealed record PluginRegistrationCollisionError(string CollisionId, string ReservedBy)
    : PluginRegistryError(
        $"Plugin id '{CollisionId}' is reserved by {ReservedBy} and cannot be registered externally.",
        CollisionId);

public sealed record PluginTerminationFailure(string PluginId, string Message, string? Detail = null);

public sealed record PluginTerminationAggregateError : PluginRegistryError
{
    public PluginTerminationAggregateError(IReadOnlyList<PluginTerminationFailure> failures)
        : base($"Failed to terminate {failures.Count} built-in plugin(s).")
    {
        Failures = failures;
    }

    public IReadOnlyList<PluginTerminationFailure> Failures { get; }
}

public sealed record PluginRegistryResult(PluginRegistryError? Error)
{
    public bool IsSuccess => Error is null;

    public static PluginRegistryResult Success() =>
        new((PluginRegistryError?)null);

    public static PluginRegistryResult Failure(PluginRegistryError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PluginRegistryResult(error);
    }
}
