using System.Globalization;
using System.Text.Json.Serialization;

namespace Luminalium.Core.Identity;

public sealed record VersionMetadata(
    [property: JsonPropertyName("code_name")] string CodeName,
    [property: JsonPropertyName("code_name_CN")] string CodeNameChinese,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("versionnm")] string VersionName,
    [property: JsonPropertyName("build")] string Build,
    [property: JsonPropertyName("future_codename")] string FutureCodename)
{
    public VersionMetadata WithNightlyVersion(DateOnly date)
    {
        var nightlyVersion = string.Concat(
            date.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture),
            "-nightly");

        return this with { Version = nightlyVersion };
    }
}
