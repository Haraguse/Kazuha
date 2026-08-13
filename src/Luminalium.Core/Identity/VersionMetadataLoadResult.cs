namespace Luminalium.Core.Identity;

public enum VersionMetadataErrorCode
{
    FileNotFound,
    ReadFailure,
    MalformedJson,
    InvalidRoot,
    MissingField,
    InvalidFieldType,
    EmptyField,
}

public sealed record VersionMetadataError(
    VersionMetadataErrorCode Code,
    string Path,
    string Message,
    string? Field = null);

public sealed record VersionMetadataLoadResult(
    VersionMetadata? Metadata,
    VersionMetadataError? Error)
{
    public bool IsSuccess => Metadata is not null;

    public static VersionMetadataLoadResult Success(VersionMetadata metadata) =>
        new(metadata, null);

    public static VersionMetadataLoadResult Failure(VersionMetadataError error) =>
        new(null, error);
}
