using System.IO.Compression;
using System.Text.RegularExpressions;
using Luminalium.Core.Identity;

namespace Luminalium.Updater;

public interface IUpdateValidator
{
    Task<UpdateOperationResult<ValidatedUpdatePackage>> ValidateAsync(
        string zipPath,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateValidator : IUpdateValidator
{
    private static readonly Regex[] ForbiddenFilePatterns = CreatePatterns(
        @"^python\d+\.dll$",
        @"^libpython\d+\.dll$",
        @"^python\d*\.exe$",
        @"^pythonw\d*\.exe$",
        @"^python\d+\.zip$",
        @"^base_library\.zip$",
        @"^Python\.Runtime\.dll$",
        @"^PySide\d*\.dll$",
        @"^shiboken\d*(\.abi3)?\.dll$",
        @"^Qt\d+[^.]*\.dll$",
        @"^QtWebEngineProcess\.exe$",
        @"^qwindows\.dll$",
        @"^libEGL\.dll$",
        @"^libGLESv2\.dll$",
        @"^qtwebengine.*\.pak$",
        @"^icudtl\.dat$",
        @"^WebView2Loader(Static)?\.dll$",
        @"^Microsoft\.Web\.WebView2\.(Core|WinForms|Wpf)\.dll$",
        @"^msedgewebview2\.exe$",
        @"^Kazuha\.PowerPointBridge\.dll$",
        @"\.vsto$",
        @"\.qml$",
        @"^qmldir$",
        @"\.pyc$",
        @"\.py$");

    private static readonly Regex[] ForbiddenDirectoryPatterns = CreatePatterns(
        @"^_internal$",
        @"^PySide\d*$",
        @"^PyQt\d*$",
        @"^shiboken\d*$",
        @"^qml$",
        @"^qtwebengine$",
        @"^plugins$",
        @"^external$",
        @"^webview2$",
        @"^EBWebView$",
        @"\.dist-info$");

    public Task<UpdateOperationResult<ValidatedUpdatePackage>> ValidateAsync(
        string zipPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(zipPath))
        {
            return Task.FromResult(UpdateOperation.Failure<ValidatedUpdatePackage>(new UpdateError(
                UpdateErrorCode.ValidationFailed,
                "The staged update archive was not found.",
                zipPath)));
        }

        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var entries = new List<string>();
            var hasExecutable = false;
            var hasVersionMetadata = false;

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalized = NormalizeEntryName(entry.FullName);
                var pathError = ValidatePath(normalized, entry.FullName);
                if (pathError is not null)
                {
                    return Task.FromResult(UpdateOperation.Failure<ValidatedUpdatePackage>(pathError));
                }

                if (normalized.Length == 0)
                {
                    continue;
                }

                entries.Add(normalized);
                var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var leafName = segments[^1];
                if (segments.Length == 1 && string.Equals(leafName, "Luminalium.exe", StringComparison.OrdinalIgnoreCase))
                {
                    hasExecutable = true;
                }

                if (segments.Length == 1 && string.Equals(leafName, ProductIdentity.VersionMetadataFileName, StringComparison.OrdinalIgnoreCase))
                {
                    hasVersionMetadata = true;
                }

                var forbiddenError = ValidateForbiddenPayload(segments, leafName);
                if (forbiddenError is not null)
                {
                    return Task.FromResult(UpdateOperation.Failure<ValidatedUpdatePackage>(forbiddenError));
                }
            }

            if (!hasExecutable || !hasVersionMetadata)
            {
                return Task.FromResult(UpdateOperation.Failure<ValidatedUpdatePackage>(new UpdateError(
                    UpdateErrorCode.ValidationFailed,
                    "The update archive must contain Luminalium.exe and version.json at the zip root.",
                    $"Luminalium.exe={hasExecutable}; version.json={hasVersionMetadata}")));
            }

            return Task.FromResult(UpdateOperation.Success(new ValidatedUpdatePackage(zipPath, entries)));
        }
        catch (InvalidDataException exception)
        {
            return Task.FromResult(UpdateOperation.Failure<ValidatedUpdatePackage>(new UpdateError(
                UpdateErrorCode.ValidationFailed,
                "The staged update archive is not a readable zip file.",
                exception.Message,
                exception)));
        }
        catch (IOException exception)
        {
            return Task.FromResult(UpdateOperation.Failure<ValidatedUpdatePackage>(new UpdateError(
                UpdateErrorCode.ValidationFailed,
                "The staged update archive could not be read.",
                exception.Message,
                exception)));
        }
    }

    private static Regex[] CreatePatterns(params string[] patterns) =>
        patterns.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)).ToArray();

    private static string NormalizeEntryName(string entryName) =>
        entryName.Replace('\\', '/').TrimEnd('/');

    private static UpdateError? ValidatePath(string normalized, string original)
    {
        if (Path.IsPathRooted(original) || (normalized.Length > 0 && normalized[0] == '/') || normalized.Contains(':', StringComparison.Ordinal))
        {
            return new UpdateError(UpdateErrorCode.ValidationFailed, "The update archive contains an absolute path entry.", original);
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => string.Equals(segment, "..", StringComparison.Ordinal)))
        {
            return new UpdateError(UpdateErrorCode.ValidationFailed, "The update archive contains a parent-directory path entry.", original);
        }

        return null;
    }

    private static UpdateError? ValidateForbiddenPayload(string[] segments, string leafName)
    {
        foreach (var pattern in ForbiddenFilePatterns)
        {
            if (pattern.IsMatch(leafName))
            {
                return new UpdateError(
                    UpdateErrorCode.ValidationFailed,
                    "The update archive contains a forbidden legacy payload file.",
                    leafName);
            }
        }

        foreach (var segment in segments.Take(Math.Max(0, segments.Length - 1)))
        {
            foreach (var pattern in ForbiddenDirectoryPatterns)
            {
                if (pattern.IsMatch(segment))
                {
                    return new UpdateError(
                        UpdateErrorCode.ValidationFailed,
                        "The update archive contains a forbidden legacy payload directory.",
                        segment);
                }
            }
        }

        return null;
    }
}
