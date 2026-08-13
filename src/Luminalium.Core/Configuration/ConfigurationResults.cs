namespace Luminalium.Core.Configuration;

public enum ConfigurationLoadOutcome
{
    Loaded,
    CreatedDefault,
    RecoveredDefault,
}

public enum ConfigurationLoadWarningCode
{
    ReadFailure,
    MalformedJson,
    InvalidRoot,
    InvalidSchemaVersion,
    InvalidSection,
    InvalidValue,
    PersistenceFailure,
}

public sealed record ConfigurationLoadWarning(
    ConfigurationLoadWarningCode Code,
    string Path,
    string Message,
    string? BackupPath = null,
    string? Field = null);

public sealed record ConfigurationLoadResult(
    LuminaliumConfig Config,
    ConfigurationLoadOutcome Outcome,
    string Path,
    ConfigurationLoadWarning? Warning)
{
    public bool IsSuccess => Outcome is not ConfigurationLoadOutcome.RecoveredDefault;

    public bool IsRecoveredDefault => Outcome is ConfigurationLoadOutcome.RecoveredDefault;

    public bool WasCreated => Outcome is ConfigurationLoadOutcome.CreatedDefault;

    public static ConfigurationLoadResult Loaded(
        LuminaliumConfig config,
        string path) =>
        new(config, ConfigurationLoadOutcome.Loaded, path, null);

    public static ConfigurationLoadResult CreatedDefault(
        LuminaliumConfig config,
        string path) =>
        new(config, ConfigurationLoadOutcome.CreatedDefault, path, null);

    public static ConfigurationLoadResult RecoveredDefault(
        LuminaliumConfig config,
        string path,
        ConfigurationLoadWarning warning) =>
        new(config, ConfigurationLoadOutcome.RecoveredDefault, path, warning);
}

public enum ConfigurationSaveErrorCode
{
    InvalidConfiguration,
    SerializationFailure,
    WriteFailure,
    ReplacementFailure,
    TemporaryFileCleanupFailure,
}

public sealed record ConfigurationSaveError(
    ConfigurationSaveErrorCode Code,
    string Path,
    string Message,
    string? Field = null);

public sealed record ConfigurationSaveResult(
    string Path,
    ConfigurationSaveError? Error)
{
    public bool IsSuccess => Error is null;

    public static ConfigurationSaveResult Success(string path) => new(path, null);

    public static ConfigurationSaveResult Failure(ConfigurationSaveError error) =>
        new(error.Path, error);
}

internal sealed record ConfigurationValidationIssue(
    ConfigurationLoadWarningCode Code,
    string Message,
    string? Field = null);
