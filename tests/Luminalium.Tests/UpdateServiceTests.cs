using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Luminalium.Updater;
using Xunit;

namespace Luminalium.Tests;

public sealed class UpdateServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"Luminalium-UpdateService-{Guid.NewGuid():N}");

    public UpdateServiceTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task DownloadValidateAndReplaceHappyPathUsesLocalFixtures()
    {
        var zipBytes = CreateUpdateZip(("Luminalium.exe", "new-exe"), ("version.json", VersionJson("2.0.0-nightly")));
        var checksum = Convert.ToHexString(SHA256.HashData(zipBytes)).ToLowerInvariant();
        await using var server = await LocalUpdateServer.StartAsync(new Dictionary<string, LocalResponse>(StringComparer.OrdinalIgnoreCase)
        {
            ["/Luminalium-Windows.zip"] = LocalResponse.Bytes(zipBytes, "application/zip"),
            ["/Luminalium-Windows.zip.sha256"] = LocalResponse.Text(checksum),
        });
        var cacheRoot = Path.Combine(_directory, "cache");
        var installDirectory = CreateInstall("old-exe", VersionJson("1.0.0"));
        var downloader = new UpdateDownloader(cacheRoot: cacheRoot);
        var progress = new List<UpdateProgress>();
        var updateInfo = new UpdateInfo(true, "nightly", "Luminalium-Windows.zip", server.Uri("/Luminalium-Windows.zip"), zipBytes.Length, "Nightly", false);

        var download = await downloader.DownloadAsync(updateInfo, new Progress<UpdateProgress>(progress.Add));
        var validation = await new UpdateValidator().ValidateAsync(download.Value!.UpdateZipPath);
        var coordinator = new UpdateReplacementCoordinator(processExitTimeout: TimeSpan.FromMilliseconds(250));
        var replacement = await coordinator.ReplaceAsync(download.Value, installDirectory);

        Assert.True(download.IsSuccess, download.Error?.Message);
        Assert.True(File.Exists(Path.Combine(cacheRoot, "nightly", "update.zip")));
        Assert.Contains(progress, item => item.Stage == UpdateProgressStage.Downloading && item.Percent == 100);
        Assert.True(validation.IsSuccess, validation.Error?.Message);
        Assert.True(replacement.IsSuccess, replacement.Error?.Message);
        Assert.False(replacement.Value!.RolledBack);
        Assert.True(Directory.Exists(replacement.Value.BackupDirectory));
        Assert.Equal("new-exe", File.ReadAllText(Path.Combine(installDirectory, "Luminalium.exe")));
        Assert.Equal("old-exe", File.ReadAllText(Path.Combine(replacement.Value.BackupDirectory, "Luminalium.exe")));
    }

    [Fact]
    public async Task BadChecksumFailsBeforeReplacementAndKeepsOriginalFiles()
    {
        var zipBytes = CreateUpdateZip(("Luminalium.exe", "new-exe"), ("version.json", VersionJson("2.0.0")));
        await using var server = await LocalUpdateServer.StartAsync(new Dictionary<string, LocalResponse>(StringComparer.OrdinalIgnoreCase)
        {
            ["/Luminalium-Windows.zip"] = LocalResponse.Bytes(zipBytes, "application/zip"),
            ["/Luminalium-Windows.zip.sha256"] = LocalResponse.Text(new string('0', 64)),
        });
        var installDirectory = CreateInstall("old-exe", VersionJson("1.0.0"));
        var downloader = new UpdateDownloader(cacheRoot: Path.Combine(_directory, "cache"));
        var updateInfo = new UpdateInfo(true, "nightly", "Luminalium-Windows.zip", server.Uri("/Luminalium-Windows.zip"), zipBytes.Length, string.Empty, false);

        var download = await downloader.DownloadAsync(updateInfo);

        Assert.False(download.IsSuccess);
        Assert.Equal(UpdateErrorCode.ChecksumMismatch, download.Error!.Code);
        Assert.Equal("old-exe", File.ReadAllText(Path.Combine(installDirectory, "Luminalium.exe")));
        Assert.Equal(VersionJson("1.0.0"), File.ReadAllText(Path.Combine(installDirectory, "version.json")));
    }

    [Fact]
    public async Task StructurallyInvalidZipFailsValidationBeforeReplacement()
    {
        var zipPath = Path.Combine(_directory, "invalid.zip");
        await File.WriteAllBytesAsync(zipPath, CreateUpdateZip(("version.json", VersionJson("2.0.0"))));
        var installDirectory = CreateInstall("old-exe", VersionJson("1.0.0"));

        var validation = await new UpdateValidator().ValidateAsync(zipPath);

        Assert.False(validation.IsSuccess);
        Assert.Equal(UpdateErrorCode.ValidationFailed, validation.Error!.Code);
        Assert.Equal("old-exe", File.ReadAllText(Path.Combine(installDirectory, "Luminalium.exe")));
        Assert.Equal(VersionJson("1.0.0"), File.ReadAllText(Path.Combine(installDirectory, "version.json")));
    }

    [Fact]
    public async Task ReplacementFailureRestoresOriginalFilesAndReturnsRollbackResult()
    {
        var zipPath = Path.Combine(_directory, "rollback.zip");
        await File.WriteAllBytesAsync(
            zipPath,
            CreateUpdateZip(("Luminalium.exe", "new-exe"), ("version.json", VersionJson("2.0.0")), ("app.dll", "new-dll")),
            CancellationToken.None);
        var installDirectory = CreateInstall("old-exe", VersionJson("1.0.0"));
        var fileSystem = new FaultingUpdateFileSystem(installDirectory, "app.dll");
        var coordinator = new UpdateReplacementCoordinator(fileSystem: fileSystem, mutexName: "Luminalium_Updater_" + Guid.NewGuid().ToString("N"));
        var staged = new StagedUpdate("2.0.0", zipPath, _directory, new FileInfo(zipPath).Length);

        var replacement = await coordinator.ReplaceAsync(staged, installDirectory);

        Assert.False(replacement.IsSuccess);
        Assert.Equal(UpdateErrorCode.RollbackSucceeded, replacement.Error!.Code);
        Assert.True(replacement.Value!.RolledBack);
        Assert.Equal("old-exe", File.ReadAllText(Path.Combine(installDirectory, "Luminalium.exe")));
        Assert.Equal(VersionJson("1.0.0"), File.ReadAllText(Path.Combine(installDirectory, "version.json")));
        Assert.False(File.Exists(Path.Combine(installDirectory, "app.dll")));
    }

    [Theory]
    [InlineData("1.4.0.9-EMERGENCY", "1.4.0.9-EMERGENCY", false, false)]
    [InlineData("1.4.0.9-EMERGENCY", "1.4.0.10-EMERGENCY", false, true)]
    [InlineData("1.4.0.9-EMERGENCY", "1.4.0.9-EMERGENCY", true, true)]
    [InlineData("2026.08.13-nightly", "nightly", false, true)]
    public async Task FeedPreservesOpaqueVersionSemantics(string currentVersion, string tag, bool force, bool expectedAvailable)
    {
        using var client = new GitHubUpdateFeedClient(
            new StaticReleaseHandler(tag),
            [new UpdateMirror("github", "https://api.github.test")],
            TimeSpan.FromSeconds(1));

        var result = await client.CheckAsync(currentVersion, force);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(expectedAvailable, result.Value!.Available);
        Assert.Equal(force, result.Value.Forced);
        Assert.Equal("Luminalium-Windows.zip", result.Value.AssetName);
        Assert.Equal(tag, result.Value.Tag);
    }

    [Fact]
    public async Task ConcurrentReplacementReturnsTypedMutexFailure()
    {
        var firstZip = Path.Combine(_directory, "first.zip");
        var secondZip = Path.Combine(_directory, "second.zip");
        await File.WriteAllBytesAsync(firstZip, CreateUpdateZip(("Luminalium.exe", "first"), ("version.json", VersionJson("2.0.0"))));
        await File.WriteAllBytesAsync(secondZip, CreateUpdateZip(("Luminalium.exe", "second"), ("version.json", VersionJson("3.0.0"))));
        var mutexName = "Luminalium_Updater_" + Guid.NewGuid().ToString("N");
        var installDirectory = CreateInstall("old-exe", VersionJson("1.0.0"));
        using var blockingFileSystem = new BlockingUpdateFileSystem();
        var firstCoordinator = new UpdateReplacementCoordinator(fileSystem: blockingFileSystem, mutexName: mutexName, processExitTimeout: TimeSpan.FromMilliseconds(250));
        var secondCoordinator = new UpdateReplacementCoordinator(mutexName: mutexName, processExitTimeout: TimeSpan.FromMilliseconds(250));

        var firstTask = Task.Run(() => firstCoordinator.ReplaceAsync(new StagedUpdate("2.0.0", firstZip, _directory, 0), installDirectory));
        Assert.True(blockingFileSystem.BlockReached.Wait(TimeSpan.FromSeconds(5)));

        var second = await secondCoordinator.ReplaceAsync(new StagedUpdate("3.0.0", secondZip, _directory, 0), installDirectory);

        blockingFileSystem.Release();
        var first = await firstTask;

        Assert.False(second.IsSuccess);
        Assert.Equal(UpdateErrorCode.MutexHeld, second.Error!.Code);
        Assert.True(first.IsSuccess, first.Error?.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string CreateInstall(string executableContent, string versionContent)
    {
        var installDirectory = Path.Combine(_directory, "install-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(installDirectory);
        File.WriteAllText(Path.Combine(installDirectory, "Luminalium.exe"), executableContent, Encoding.UTF8);
        File.WriteAllText(Path.Combine(installDirectory, "version.json"), versionContent, Encoding.UTF8);
        return installDirectory;
    }

    private static byte[] CreateUpdateZip(params (string Name, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in entries)
            {
                var zipEntry = archive.CreateEntry(entry.Name);
                using var writer = new StreamWriter(zipEntry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(entry.Content);
            }
        }

        return stream.ToArray();
    }

    private static string VersionJson(string version) =>
        $$"""
        {
          "code_name": "Momokan",
          "code_name_CN": "Momokan",
          "version": "{{version}}",
          "versionnm": "{{version}}",
          "build": "test",
          "future_codename": "RyouYamada"
        }
        """;

    private sealed class StaticReleaseHandler(string tag) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = $$"""
            {
              "tag_name": "{{tag}}",
              "body": "fixture changelog",
              "assets": [
                { "name": "installer.exe", "browser_download_url": "https://github.com/SECTL/Luminalium/releases/download/{{tag}}/installer.exe", "size": 9 },
                { "name": "Luminalium-Windows.zip", "browser_download_url": "https://github.com/SECTL/Luminalium/releases/download/{{tag}}/Luminalium-Windows.zip", "size": 42 }
              ]
            }
            """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
                RequestMessage = request,
            });
        }
    }

    private sealed record LocalResponse(byte[] Body, string ContentType)
    {
        public static LocalResponse Bytes(byte[] body, string contentType) => new(body, contentType);

        public static LocalResponse Text(string body) => new(Encoding.UTF8.GetBytes(body), "text/plain");
    }

    private sealed class LocalUpdateServer : IAsyncDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Task _serverTask;
        private readonly IReadOnlyDictionary<string, LocalResponse> _responses;

        private LocalUpdateServer(int port, IReadOnlyDictionary<string, LocalResponse> responses)
        {
            Port = port;
            _responses = responses;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _serverTask = Task.Run(ServeAsync);
        }

        public int Port { get; }

        public static Task<LocalUpdateServer> StartAsync(IReadOnlyDictionary<string, LocalResponse> responses) =>
            Task.FromResult(new LocalUpdateServer(GetFreePort(), responses));

        public Uri Uri(string path) => new($"http://127.0.0.1:{Port}{path}", UriKind.Absolute);

        public async ValueTask DisposeAsync()
        {
            _cancellation.Cancel();
            _listener.Stop();
            _listener.Close();

            try
            {
                await _serverTask.ConfigureAwait(false);
            }
            catch (HttpListenerException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            _cancellation.Dispose();
        }

        private async Task ServeAsync()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    return;
                }

                _ = Task.Run(() => RespondAsync(context), _cancellation.Token);
            }
        }

        private async Task RespondAsync(HttpListenerContext context)
        {
            var path = context.Request.Url?.AbsolutePath ?? string.Empty;
            if (!_responses.TryGetValue(path, out var response))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            context.Response.StatusCode = 200;
            context.Response.ContentType = response.ContentType;
            context.Response.ContentLength64 = response.Body.Length;
            await context.Response.OutputStream.WriteAsync(response.Body, _cancellation.Token).ConfigureAwait(false);
            context.Response.Close();
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }

    private class DelegatingUpdateFileSystem : IUpdateFileSystem
    {
        public virtual bool DirectoryExists(string path) => Directory.Exists(path);

        public virtual bool FileExists(string path) => File.Exists(path);

        public virtual void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public virtual IEnumerable<string> EnumerateFiles(string directory) =>
            Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories) : [];

        public virtual void CopyFile(string sourcePath, string destinationPath, bool overwrite)
        {
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(sourcePath, destinationPath, overwrite);
        }

        public virtual void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class FaultingUpdateFileSystem(string installDirectory, string failingLeafName) : DelegatingUpdateFileSystem
    {
        public override void CopyFile(string sourcePath, string destinationPath, bool overwrite)
        {
            if (destinationPath.StartsWith(installDirectory, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Path.GetFileName(destinationPath), failingLeafName, StringComparison.OrdinalIgnoreCase) &&
                !sourcePath.StartsWith(installDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Injected replacement copy failure.");
            }

            base.CopyFile(sourcePath, destinationPath, overwrite);
        }
    }

    private sealed class BlockingUpdateFileSystem : DelegatingUpdateFileSystem, IDisposable
    {
        private readonly ManualResetEventSlim _release = new();
        private int _blocked;

        public ManualResetEventSlim BlockReached { get; } = new();

        public override void CopyFile(string sourcePath, string destinationPath, bool overwrite)
        {
            if (Interlocked.Exchange(ref _blocked, 1) == 0)
            {
                BlockReached.Set();
                Assert.True(_release.Wait(TimeSpan.FromSeconds(5)));
            }

            base.CopyFile(sourcePath, destinationPath, overwrite);
        }

        public void Release() => _release.Set();

        public void Dispose()
        {
            _release.Dispose();
            BlockReached.Dispose();
        }
    }
}
