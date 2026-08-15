using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Luminalium.Updater;

public sealed class UpdateDownloader : IDisposable
{
    private const int BufferSize = 81920;
    private static readonly Regex Sha256Regex = new("^[a-fA-F0-9]{64}$", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly string _cacheRoot;
    private readonly int _maxAttempts;

    public UpdateDownloader(HttpMessageHandler? handler = null, string? cacheRoot = null, int maxAttempts = 3)
    {
        _httpClient = handler is null ? new HttpClient() : new HttpClient(handler);
        _cacheRoot = cacheRoot ?? CreateDefaultCacheRoot();
        _maxAttempts = maxAttempts;
    }

    public async Task<UpdateOperationResult<StagedUpdate>> DownloadAsync(
        UpdateInfo updateInfo,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!updateInfo.Available || updateInfo.DownloadUrl is null)
        {
            return UpdateOperation.Failure<StagedUpdate>(new UpdateError(
                UpdateErrorCode.UpToDate,
                "No downloadable update is available."));
        }

        var cacheDirectory = Path.Combine(_cacheRoot, SanitizePathSegment(updateInfo.Tag));
        var zipPath = Path.Combine(cacheDirectory, "update.zip");
        Directory.CreateDirectory(cacheDirectory);

        Exception? lastException = null;
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                progress?.Report(new UpdateProgress(UpdateProgressStage.Downloading, 0, $"Downloading {updateInfo.AssetName ?? "update"}..."));
                var bytes = await DownloadFileAsync(updateInfo.DownloadUrl, zipPath, progress, cancellationToken).ConfigureAwait(false);
                var checksumResult = await VerifyOptionalChecksumAsync(updateInfo.DownloadUrl, zipPath, cancellationToken).ConfigureAwait(false);
                if (!checksumResult.IsSuccess)
                {
                    return UpdateOperation.Failure<StagedUpdate>(checksumResult.Error!);
                }

                progress?.Report(new UpdateProgress(UpdateProgressStage.Downloading, 100, "Download complete.", bytes, bytes));
                return UpdateOperation.Success(new StagedUpdate(updateInfo.Tag, zipPath, cacheDirectory, bytes));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is IOException or HttpRequestException or UnauthorizedAccessException or TaskCanceledException)
            {
                lastException = exception;
                if (attempt < _maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        progress?.Report(new UpdateProgress(UpdateProgressStage.Failed, 0, "Download failed."));
        return UpdateOperation.Failure<StagedUpdate>(new UpdateError(
            UpdateErrorCode.DownloadFailed,
            $"The update could not be downloaded after {_maxAttempts.ToString(CultureInfo.InvariantCulture)} attempt(s).",
            lastException?.Message,
            lastException));
    }

    public void Dispose() => _httpClient.Dispose();

    private static string CreateDefaultCacheRoot()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = Path.GetTempPath();
        }

        return Path.Combine(localAppData, "Luminalium", "update_cache");
    }

    private static string SanitizePathSegment(string tag)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = tag.Select(character => invalid.Contains(character) ? '_' : character).ToArray();
        var sanitized = new string(chars).Trim();
        return string.IsNullOrEmpty(sanitized) ? "update" : sanitized;
    }

    private async Task<long> DownloadFileAsync(
        Uri downloadUri,
        string zipPath,
        IProgress<UpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUri);
        request.Headers.UserAgent.ParseAdd(GitHubUpdateFeedClient.ChromeUserAgent);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

        var buffer = new byte[BufferSize];
        long received = 0;
        while (true)
        {
            var read = await responseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            received += read;

            var percent = totalBytes is > 0
                ? Math.Clamp((int)(received * 100 / totalBytes.Value), 0, 99)
                : 0;
            progress?.Report(new UpdateProgress(UpdateProgressStage.Downloading, percent, "Downloading update...", received, totalBytes));
        }

        return received;
    }

    private async Task<UpdateOperationResult> VerifyOptionalChecksumAsync(
        Uri downloadUri,
        string zipPath,
        CancellationToken cancellationToken)
    {
        var sidecarUri = new Uri(downloadUri.AbsoluteUri + ".sha256", UriKind.Absolute);
        using var request = new HttpRequestMessage(HttpMethod.Get, sidecarUri);
        request.Headers.UserAgent.ParseAdd(GitHubUpdateFeedClient.ChromeUserAgent);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is not HttpStatusCode.OK)
        {
            return UpdateOperationResult.Failure(new UpdateError(
                UpdateErrorCode.ChecksumMismatch,
                "The update checksum sidecar was unavailable.",
                $"HTTP {(int)response.StatusCode} from {sidecarUri}."));
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var expected = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (expected is null || !Sha256Regex.IsMatch(expected))
        {
            return UpdateOperationResult.Failure(new UpdateError(
                UpdateErrorCode.ChecksumMismatch,
                "The update checksum sidecar did not contain a valid SHA-256 digest."));
        }

        await using var fileStream = File.OpenRead(zipPath);
        var actualBytes = await SHA256.HashDataAsync(fileStream, cancellationToken).ConfigureAwait(false);
        var actual = Convert.ToHexString(actualBytes).ToLowerInvariant();

        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
            ? UpdateOperationResult.Success()
            : UpdateOperationResult.Failure(new UpdateError(
                UpdateErrorCode.ChecksumMismatch,
                "The update archive SHA-256 checksum did not match the release sidecar.",
                $"Expected {expected}; actual {actual}."));
    }
}
