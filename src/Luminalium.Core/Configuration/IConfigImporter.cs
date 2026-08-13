namespace Luminalium.Core.Configuration;

/// <summary>
/// Stable extension seam for one-off configuration import tools.
/// </summary>
/// <remarks>
/// Python configuration import is explicitly out of scope for this release.
/// The runtime <see cref="ConfigurationService"/> never reads the Python
/// settings directory or invokes an importer. A future tool can implement this
/// interface and produce the versioned runtime model without changing it.
/// </remarks>
public interface IConfigImporter
{
    ConfigurationImportResult Import(string sourcePath);
}

public enum ConfigurationImportErrorCode
{
    InvalidSource,
    UnsupportedSource,
    ConversionFailed,
}

public sealed record ConfigurationImportError(
    ConfigurationImportErrorCode Code,
    string Path,
    string Message);

public sealed record ConfigurationImportResult(
    LuminaliumConfig? Config,
    ConfigurationImportError? Error)
{
    public bool IsSuccess => Config is not null;

    public static ConfigurationImportResult Success(LuminaliumConfig config) =>
        new(config, null);

    public static ConfigurationImportResult Failure(ConfigurationImportError error) =>
        new(null, error);
}
