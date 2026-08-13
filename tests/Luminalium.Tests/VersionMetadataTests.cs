using System.Text;
using Luminalium.Core.Identity;
using Xunit;

namespace Luminalium.Tests;

public sealed class VersionMetadataTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"Luminalium-VersionMetadata-{Guid.NewGuid():N}");

    public VersionMetadataTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void LoadPreservesOpaqueVersionAndBuildStrings()
    {
        var path = WriteFixture(
            "valid.json",
            """
            {
              "code_name": "Momokan",
              "code_name_CN": "桃缶（河原木桃香）",
              "version": "1.4.0.9-EMERGENCY",
              "versionnm": "1.4.0.9-EMERGENCY",
              "build": "00611.1409",
              "future_codename": "RyouYamada"
            }
            """);

        var result = VersionMetadataReader.Load(path);

        Assert.True(result.IsSuccess);
        Assert.Equal("1.4.0.9-EMERGENCY", result.Metadata!.Version);
        Assert.Equal("1.4.0.9-EMERGENCY", result.Metadata.VersionName);
        Assert.Equal("00611.1409", result.Metadata.Build);
        Assert.Equal("桃缶（河原木桃香）", result.Metadata.CodeNameChinese);
    }

    [Fact]
    public void NightlyMutationChangesOnlyVersion()
    {
        var metadata = new VersionMetadata(
            "Momokan",
            "桃缶（河原木桃香）",
            "1.4.0.9-EMERGENCY",
            "1.4.0.9-EMERGENCY",
            "00611.1409",
            "RyouYamada");

        var nightly = metadata.WithNightlyVersion(new DateOnly(2026, 8, 13));

        Assert.Equal("2026.08.13-nightly", nightly.Version);
        Assert.Equal(metadata.CodeName, nightly.CodeName);
        Assert.Equal(metadata.CodeNameChinese, nightly.CodeNameChinese);
        Assert.Equal(metadata.VersionName, nightly.VersionName);
        Assert.Equal(metadata.Build, nightly.Build);
        Assert.Equal(metadata.FutureCodename, nightly.FutureCodename);
    }

    [Fact]
    public void MissingVersionReturnsTypedFieldError()
    {
        var path = WriteFixture(
            "missing-version.json",
            """
            {
              "code_name": "Momokan",
              "code_name_CN": "桃缶（河原木桃香）",
              "versionnm": "1.4.0.9-EMERGENCY",
              "build": "00611.1409",
              "future_codename": "RyouYamada"
            }
            """);

        var result = VersionMetadataReader.Load(path);

        Assert.False(result.IsSuccess);
        Assert.Equal(VersionMetadataErrorCode.MissingField, result.Error!.Code);
        Assert.Equal("version", result.Error.Field);
        Assert.Equal(path, result.Error.Path);
    }

    [Fact]
    public void MalformedJsonReturnsLocationAwareError()
    {
        var path = WriteFixture("malformed.json", "{not-json");

        var result = VersionMetadataReader.Load(path);

        Assert.False(result.IsSuccess);
        Assert.Equal(VersionMetadataErrorCode.MalformedJson, result.Error!.Code);
        Assert.Contains("line", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(path, result.Error.Path);
    }

    [Fact]
    public void MissingFileReturnsTypedPathError()
    {
        var path = Path.Combine(_directory, "does-not-exist.json");

        var result = VersionMetadataReader.Load(path);

        Assert.False(result.IsSuccess);
        Assert.Equal(VersionMetadataErrorCode.FileNotFound, result.Error!.Code);
        Assert.Equal(path, result.Error.Path);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string WriteFixture(string fileName, string content)
    {
        var path = Path.Combine(_directory, fileName);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }
}
