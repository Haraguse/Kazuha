using Luminalium.Core.Media;
using Luminalium.Core.Platform;
using Xunit;

namespace Luminalium.Tests;

public sealed class SmtcServiceTests
{
    [Fact]
    public async Task HappyPathReturnsAdapterSnapshotExactly()
    {
        var snapshot = new SmtcSessionSnapshot(
            "chrome.exe",
            SmtcPlaybackStatus.Playing,
            "Track Title",
            "Track Artist",
            42_000,
            180_000,
            "chrome.exe|Track Title|Track Artist|Album",
            "data:image/png;base64,AAAA",
            true);
        var service = new SmtcSessionService(new FakeSmtcSessionAdapter(
            PlatformOperation.Success(snapshot)));

        var result = await service.GetCurrentSessionAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(snapshot, result.Value);
        Assert.Equal("chrome.exe", result.Value.SourceAppUserModelId);
        Assert.Equal(SmtcPlaybackStatus.Playing, result.Value.PlaybackStatus);
        Assert.Equal("Track Title", result.Value.Title);
        Assert.Equal("Track Artist", result.Value.Artist);
        Assert.Equal(42_000, result.Value.PositionMs);
        Assert.Equal(180_000, result.Value.DurationMs);
        Assert.Equal("chrome.exe|Track Title|Track Artist|Album", result.Value.ArtworkKey);
        Assert.Equal("data:image/png;base64,AAAA", result.Value.ArtworkDataUrl);
        Assert.True(result.Value.IsCurrent);
    }

    [Fact]
    public async Task NoSessionReturnsEmptySnapshotSuccess()
    {
        var service = new SmtcSessionService(new FakeSmtcSessionAdapter(
            PlatformOperation.Success(SmtcSessionSnapshot.Empty)));

        var result = await service.TryGetSnapshotAsync();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(SmtcSessionSnapshot.Empty, result.Value);
    }

    [Fact]
    public async Task UnavailableTypedErrorIsForwardedWithoutThrowing()
    {
        var error = new PlatformOperationError(
            PlatformOperationErrorCode.Unavailable,
            "SMTC unavailable",
            "test detail");
        var service = new SmtcSessionService(new FakeSmtcSessionAdapter(
            PlatformOperation.Failure<SmtcSessionSnapshot>(error)));

        var result = await service.GetCurrentSessionAsync();

        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
        Assert.Equal(PlatformOperationErrorCode.Unavailable, result.Error!.Code);
    }

    [Fact]
    public async Task RepeatedReadsCallAdapterOncePerReadWithoutProcessState()
    {
        var snapshot = new SmtcSessionSnapshot(
            "msedge.exe",
            SmtcPlaybackStatus.Paused,
            "Paused Title",
            "Paused Artist",
            3_000,
            9_000,
            "msedge.exe|Paused Title|Paused Artist|Paused Album",
            string.Empty,
            false);
        var adapter = new FakeSmtcSessionAdapter(PlatformOperation.Success(snapshot));
        var service = new SmtcSessionService(adapter);

        var first = await service.TryGetSnapshotAsync();
        var second = await service.TryGetSnapshotAsync();

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(snapshot, first.Value);
        Assert.Equal(snapshot, second.Value);
        Assert.Equal(2, adapter.CallCount);
    }

    [Fact]
    public void PureHelpersMatchLegacyDeterministicBehavior()
    {
        Assert.Equal(0, SmtcMetadata.RankStatus(SmtcPlaybackStatus.Playing));
        Assert.Equal(1, SmtcMetadata.RankStatus(SmtcPlaybackStatus.Paused));
        Assert.Equal(2, SmtcMetadata.RankStatus(SmtcPlaybackStatus.Changing));
        Assert.Equal(3, SmtcMetadata.RankStatus(SmtcPlaybackStatus.Stopped));
        Assert.Equal(4, SmtcMetadata.RankStatus(SmtcPlaybackStatus.Unknown));

        Assert.Equal(20, SmtcMetadata.GetMetadataScore(
            "title",
            "artist",
            "album",
            "subtitle",
            "album artist",
            [1]));
        Assert.Equal(0, SmtcMetadata.GetMetadataScore(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            null));

        Assert.Equal("title", SmtcMetadata.FirstNonEmpty(" ", " title ", "fallback"));
        Assert.Equal("genre", SmtcMetadata.GetFirstGenre([" ", " genre ", "other"]));
        Assert.Equal("", SmtcMetadata.GetFirstGenre(null));

        Assert.Equal("Microsoft Edge", SmtcMetadata.GetFriendlySourceName("msedge.exe!App"));
        Assert.Equal("Google Chrome", SmtcMetadata.GetFriendlySourceName(@"C:\Program Files\Google\Chrome\chrome.exe"));
        Assert.Equal("Mozilla Firefox", SmtcMetadata.GetFriendlySourceName("firefox.exe"));
        Assert.Equal("Application Frame Host", SmtcMetadata.GetFriendlySourceName("ApplicationFrameHost.exe"));
        Assert.Equal("custom.exe", SmtcMetadata.GetFriendlySourceName(" custom.exe "));

        Assert.Equal("", SmtcMetadata.NormalizeContentType(" application/octet-stream; charset=binary "));
        Assert.Equal("image/jpeg", SmtcMetadata.NormalizeContentType("image/jpg"));
        Assert.Equal("image/jpeg", SmtcMetadata.NormalizeContentType("IMAGE/JFIF"));
        Assert.Equal("image/png", SmtcMetadata.NormalizeContentType("image/x-png"));
        Assert.Equal("image/bmp", SmtcMetadata.NormalizeContentType("image/x-ms-bmp"));
        Assert.Equal("image/x-icon", SmtcMetadata.NormalizeContentType("image/vnd.microsoft.icon"));
        Assert.Equal("image/svg+xml", SmtcMetadata.NormalizeContentType("image/svg"));

        Assert.Equal("image/png", SmtcMetadata.DetectContentType([0x89, 0x50, 0x4E, 0x47]));
        Assert.Equal("image/jpeg", SmtcMetadata.DetectContentType([0xFF, 0xD8, 0x00, 0x00]));
        Assert.Equal("image/webp", SmtcMetadata.DetectContentType([
            0x52, 0x49, 0x46, 0x46,
            0x00, 0x00, 0x00, 0x00,
            0x57, 0x45, 0x42, 0x50]));
        Assert.Equal("image/bmp", SmtcMetadata.DetectContentType([0x42, 0x4D, 0x00, 0x00]));
        Assert.Equal("image/gif", SmtcMetadata.DetectContentType([0x47, 0x49, 0x46, 0x38]));

        Assert.True(SmtcMetadata.LooksLikeSvg("  <svg viewBox=\"0 0 1 1\"></svg>"u8.ToArray()));
        Assert.Equal("source|title|artist|album", SmtcMetadata.BuildArtworkKey(
            "source",
            "title",
            "artist",
            "album"));
    }

    private sealed class FakeSmtcSessionAdapter : ISmtcSessionAdapter
    {
        private readonly PlatformOperationResult<SmtcSessionSnapshot> _result;

        public FakeSmtcSessionAdapter(PlatformOperationResult<SmtcSessionSnapshot> result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public Task<PlatformOperationResult<SmtcSessionSnapshot>> TryGetSnapshotAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }
}
