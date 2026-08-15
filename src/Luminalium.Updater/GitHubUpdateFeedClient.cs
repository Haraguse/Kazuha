using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Luminalium.Core.Identity;

namespace Luminalium.Updater;

public interface IUpdateFeedClient
{
    Task<UpdateOperationResult<UpdateInfo>> CheckAsync(string currentVersion, bool force, CancellationToken cancellationToken = default);
}

public sealed class GitHubUpdateFeedClient : IUpdateFeedClient, IDisposable
{
    public const string Repository = "SECTL/Luminalium";
    public const string ChromeUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36";

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
    };

    private readonly HttpClient _httpClient;
    private readonly IReadOnlyList<UpdateMirror> _mirrors;
    private readonly TimeSpan _mirrorTimeout;

    public GitHubUpdateFeedClient(
        HttpMessageHandler? handler = null,
        IEnumerable<UpdateMirror>? mirrors = null,
        TimeSpan? mirrorTimeout = null)
    {
        _httpClient = handler is null ? new HttpClient() : new HttpClient(handler);
        _mirrors = (mirrors ?? CreateDefaultMirrors()).ToArray();
        _mirrorTimeout = mirrorTimeout ?? TimeSpan.FromSeconds(15);
    }

    public async Task<UpdateOperationResult<UpdateInfo>> CheckAsync(
        string currentVersion,
        bool force,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        foreach (var mirror in _mirrors)
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(_mirrorTimeout);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, BuildReleaseUri(mirror));
                request.Headers.UserAgent.ParseAdd(ChromeUserAgent);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token).ConfigureAwait(false);
                if (response.StatusCode is not HttpStatusCode.OK)
                {
                    errors.Add($"{mirror.Name}: HTTP {(int)response.StatusCode}");
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(timeoutSource.Token).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(stream, DocumentOptions, timeoutSource.Token).ConfigureAwait(false);
                return ParseRelease(document.RootElement, currentVersion, force);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                errors.Add($"{mirror.Name}: timed out after {_mirrorTimeout.TotalSeconds:0}s ({exception.Message})");
            }
            catch (HttpRequestException exception)
            {
                errors.Add($"{mirror.Name}: {exception.Message}");
            }
            catch (JsonException exception)
            {
                errors.Add($"{mirror.Name}: malformed release JSON ({exception.Message})");
            }
        }

        return UpdateOperation.Failure<UpdateInfo>(new UpdateError(
            UpdateErrorCode.FeedUnavailable,
            "No update feed mirror returned a usable latest release.",
            string.Join(Environment.NewLine, errors)));
    }

    public void Dispose() => _httpClient.Dispose();

    public static IReadOnlyList<UpdateMirror> CreateDefaultMirrors() =>
    [
        new("github", "https://api.github.com"),
    ];

    private static Uri BuildReleaseUri(UpdateMirror mirror) =>
        new($"{mirror.ApiBaseUrl.TrimEnd('/')}/repos/{Repository}/releases/latest", UriKind.Absolute);

    private static UpdateOperationResult<UpdateInfo> ParseRelease(
        JsonElement root,
        string currentVersion,
        bool force)
    {
        if (!root.TryGetProperty("tag_name", out var tagElement) || tagElement.ValueKind is not JsonValueKind.String)
        {
            return UpdateOperation.Failure<UpdateInfo>(new UpdateError(
                UpdateErrorCode.ReleaseMalformed,
                "The latest release response did not contain a string tag_name."));
        }

        var tag = tagElement.GetString();
        if (string.IsNullOrWhiteSpace(tag))
        {
            return UpdateOperation.Failure<UpdateInfo>(new UpdateError(
                UpdateErrorCode.ReleaseMalformed,
                "The latest release tag_name was empty."));
        }

        if (!root.TryGetProperty("assets", out var assetsElement) || assetsElement.ValueKind is not JsonValueKind.Array)
        {
            return UpdateOperation.Failure<UpdateInfo>(new UpdateError(
                UpdateErrorCode.AssetUnavailable,
                "The latest release response did not contain an assets array."));
        }

        var asset = SelectAsset(assetsElement);
        if (asset is null)
        {
            return UpdateOperation.Failure<UpdateInfo>(new UpdateError(
                UpdateErrorCode.AssetUnavailable,
                $"No {ProductIdentity.WindowsArtifactName} asset was found on the latest release."));
        }

        var available = force || !string.Equals(tag, currentVersion, StringComparison.OrdinalIgnoreCase);
        var changelog = root.TryGetProperty("body", out var bodyElement) && bodyElement.ValueKind is JsonValueKind.String
            ? bodyElement.GetString() ?? string.Empty
            : string.Empty;

        if (!TryParseCanonicalDownloadUrl(asset.Value.DownloadUrl, tag, out var downloadUrl))
        {
            return UpdateOperation.Failure<UpdateInfo>(new UpdateError(
                UpdateErrorCode.AssetUnavailable,
                $"The {ProductIdentity.WindowsArtifactName} asset URL was not a canonical GitHub release download URL."));
        }

        var updateInfo = new UpdateInfo(
            available,
            tag,
            asset.Value.Name,
            downloadUrl,
            asset.Value.Size,
            changelog,
            force);

        return UpdateOperation.Success(updateInfo);
    }

    private static ReleaseAsset? SelectAsset(JsonElement assetsElement)
    {
        foreach (var assetElement in assetsElement.EnumerateArray())
        {
            if (!TryReadAsset(assetElement, out var asset))
            {
                continue;
            }

            if (string.Equals(asset.Name, ProductIdentity.WindowsArtifactName, StringComparison.Ordinal))
            {
                return asset;
            }
        }

        return null;
    }

    private static bool TryReadAsset(JsonElement assetElement, out ReleaseAsset asset)
    {
        asset = default;
        if (!assetElement.TryGetProperty("name", out var nameElement) || nameElement.ValueKind is not JsonValueKind.String ||
            !assetElement.TryGetProperty("browser_download_url", out var urlElement) || urlElement.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        var name = nameElement.GetString();
        var downloadUrl = urlElement.GetString();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(downloadUrl))
        {
            return false;
        }

        var size = assetElement.TryGetProperty("size", out var sizeElement) && sizeElement.TryGetInt64(out var parsedSize)
            ? parsedSize
            : (long?)null;

        asset = new ReleaseAsset(name, downloadUrl, size);
        return true;
    }

    private static bool TryParseCanonicalDownloadUrl(string downloadUrl, string tag, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var parsed) ||
            parsed.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(parsed.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
            parsed.Port != 443 ||
            !string.IsNullOrEmpty(parsed.UserInfo) ||
            !string.IsNullOrEmpty(parsed.Fragment) ||
            !string.IsNullOrEmpty(parsed.Query))
        {
            return false;
        }

        var expectedPath = $"/SECTL/Luminalium/releases/download/{tag}/{ProductIdentity.WindowsArtifactName}";
        if (!string.Equals(parsed.AbsolutePath, expectedPath, StringComparison.Ordinal))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private readonly record struct ReleaseAsset(string Name, string DownloadUrl, long? Size);
}
