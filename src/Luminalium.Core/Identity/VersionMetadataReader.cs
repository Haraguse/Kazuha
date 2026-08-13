using System.Text.Json;

namespace Luminalium.Core.Identity;

public static class VersionMetadataReader
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowDuplicateProperties = false,
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
    };

    public static VersionMetadataLoadResult Load(string path)
    {
        if (!File.Exists(path))
        {
            return Failure(
                VersionMetadataErrorCode.FileNotFound,
                path,
                $"Version metadata file was not found: {path}");
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException exception)
        {
            return Failure(
                VersionMetadataErrorCode.ReadFailure,
                path,
                $"Version metadata could not be read: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(
                VersionMetadataErrorCode.ReadFailure,
                path,
                $"Version metadata could not be read: {exception.Message}");
        }

        try
        {
            using var document = JsonDocument.Parse(json, DocumentOptions);
            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                return Failure(
                    VersionMetadataErrorCode.InvalidRoot,
                    path,
                    "Version metadata must be a JSON object.");
            }

            var root = document.RootElement;
            if (!TryReadRequiredString(root, "code_name", path, out var codeName, out var error) ||
                !TryReadRequiredString(root, "code_name_CN", path, out var codeNameChinese, out error) ||
                !TryReadRequiredString(root, "version", path, out var version, out error) ||
                !TryReadRequiredString(root, "versionnm", path, out var versionName, out error) ||
                !TryReadRequiredString(root, "build", path, out var build, out error) ||
                !TryReadRequiredString(root, "future_codename", path, out var futureCodename, out error))
            {
                return VersionMetadataLoadResult.Failure(error!);
            }

            return VersionMetadataLoadResult.Success(new VersionMetadata(
                codeName!,
                codeNameChinese!,
                version!,
                versionName!,
                build!,
                futureCodename!));
        }
        catch (JsonException exception)
        {
            return Failure(
                VersionMetadataErrorCode.MalformedJson,
                path,
                $"Version metadata is malformed JSON at line {exception.LineNumber}, byte {exception.BytePositionInLine}: {exception.Message}");
        }
    }

    private static bool TryReadRequiredString(
        JsonElement root,
        string field,
        string path,
        out string? value,
        out VersionMetadataError? error)
    {
        value = null;
        error = null;

        if (!root.TryGetProperty(field, out var property))
        {
            error = new VersionMetadataError(
                VersionMetadataErrorCode.MissingField,
                path,
                $"Version metadata is missing required field '{field}'.",
                field);
            return false;
        }

        if (property.ValueKind is not JsonValueKind.String)
        {
            error = new VersionMetadataError(
                VersionMetadataErrorCode.InvalidFieldType,
                path,
                $"Version metadata field '{field}' must be a string.",
                field);
            return false;
        }

        value = property.GetString();
        if (string.IsNullOrEmpty(value))
        {
            error = new VersionMetadataError(
                VersionMetadataErrorCode.EmptyField,
                path,
                $"Version metadata field '{field}' must not be empty.",
                field);
            return false;
        }

        return true;
    }

    private static VersionMetadataLoadResult Failure(
        VersionMetadataErrorCode code,
        string path,
        string message) =>
        VersionMetadataLoadResult.Failure(new VersionMetadataError(code, path, message));
}
