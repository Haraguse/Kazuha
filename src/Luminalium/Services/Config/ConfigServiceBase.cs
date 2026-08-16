using System.Text.Encodings.Web;
using System.Text.Json;
using Luminalium.Models.Config;

namespace Luminalium.Services.Config;

public abstract class ConfigServiceBase
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        // PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public abstract bool IsConfigExists<T>(T fallback) where T : ConfigBase;
    public abstract T LoadConfig<T>(T fallback) where T : ConfigBase;
    public abstract void SaveConfig<T>(T config) where T : ConfigBase;
    public abstract void DeleteConfig<T>(T config) where T : ConfigBase;
}