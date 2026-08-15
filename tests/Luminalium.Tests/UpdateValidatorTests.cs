using System.IO.Compression;
using System.Text;
using Luminalium.Updater;
using Xunit;

namespace Luminalium.Tests;

/// <summary>
/// T21 negative fixtures for the update archive validator: corrupted archives,
/// path traversal, and forbidden legacy payloads must all fail closed with a
/// typed <see cref="UpdateErrorCode.ValidationFailed"/> result.
/// </summary>
public sealed class UpdateValidatorTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"Luminalium-UpdateValidator-{Guid.NewGuid():N}");

    public UpdateValidatorTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task ValidArchivePassesValidation()
    {
        var zipPath = WriteZip(("Luminalium.exe", "new-exe"), ("version.json", VersionJson("2.0.0")));

        var result = await new UpdateValidator().ValidateAsync(zipPath);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Contains("Luminalium.exe", result.Value!.Entries);
        Assert.Contains("version.json", result.Value.Entries);
    }

    [Theory]
    [InlineData("/absolute.exe")]
    [InlineData("C:\\absolute.exe")]
    [InlineData("sub:/weird.exe")]
    public async Task AbsolutePathEntriesAreRejected(string entryName)
    {
        var zipPath = WriteZip((entryName, "x"), ("Luminalium.exe", "exe"), ("version.json", VersionJson("2.0.0")));

        var result = await new UpdateValidator().ValidateAsync(zipPath);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateErrorCode.ValidationFailed, result.Error!.Code);
        Assert.Contains("absolute path", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("../escape.exe")]
    [InlineData("sub/../escape.exe")]
    [InlineData("..\\escape.exe")]
    public async Task ParentDirectoryEntriesAreRejected(string entryName)
    {
        var zipPath = WriteZip((entryName, "x"), ("Luminalium.exe", "exe"), ("version.json", VersionJson("2.0.0")));

        var result = await new UpdateValidator().ValidateAsync(zipPath);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateErrorCode.ValidationFailed, result.Error!.Code);
        Assert.Contains("parent-directory", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("python312.dll")]
    [InlineData("libpython312.dll")]
    [InlineData("PySide6.dll")]
    [InlineData("Qt6Core.dll")]
    [InlineData("WebView2Loader.dll")]
    [InlineData("helper.py")]
    [InlineData("style.qml")]
    [InlineData("addin.vsto")]
    public async Task ForbiddenPayloadFilesAreRejected(string leafName)
    {
        var zipPath = WriteZip((leafName, "x"), ("Luminalium.exe", "exe"), ("version.json", VersionJson("2.0.0")));

        var result = await new UpdateValidator().ValidateAsync(zipPath);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateErrorCode.ValidationFailed, result.Error!.Code);
        Assert.Contains("forbidden legacy payload", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("_internal/Luminalium.exe")]
    [InlineData("plugins/addon.dll")]
    [InlineData("external/thing.exe")]
    [InlineData("PySide6/resources.bin")]
    [InlineData("qml/Main.qml")]
    public async Task ForbiddenPayloadDirectoriesAreRejected(string entryName)
    {
        var zipPath = WriteZip((entryName, "x"), ("Luminalium.exe", "exe"), ("version.json", VersionJson("2.0.0")));

        var result = await new UpdateValidator().ValidateAsync(zipPath);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateErrorCode.ValidationFailed, result.Error!.Code);
        Assert.Contains("forbidden legacy payload", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingRequiredRootFilesAreRejected()
    {
        var zipPath = WriteZip(("README.txt", "x"));

        var result = await new UpdateValidator().ValidateAsync(zipPath);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateErrorCode.ValidationFailed, result.Error!.Code);
        Assert.Contains("must contain", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingArchiveIsRejected()
    {
        var result = await new UpdateValidator().ValidateAsync(Path.Combine(_directory, "does-not-exist.zip"));

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateErrorCode.ValidationFailed, result.Error!.Code);
    }

    [Fact]
    public async Task UnreadableArchiveIsRejected()
    {
        var zipPath = Path.Combine(_directory, "garbage.zip");
        await File.WriteAllBytesAsync(zipPath, Encoding.UTF8.GetBytes("this is not a zip"), CancellationToken.None);

        var result = await new UpdateValidator().ValidateAsync(zipPath);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateErrorCode.ValidationFailed, result.Error!.Code);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string WriteZip(params (string Name, string Content)[] entries)
    {
        var path = Path.Combine(_directory, "update-" + Guid.NewGuid().ToString("N") + ".zip");
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

        File.WriteAllBytes(path, stream.ToArray());
        return path;
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
}
