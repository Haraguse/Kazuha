using System.Reflection;
using System.Text.Json;

namespace Luminalium.Core.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private readonly Dictionary<AppLanguage, IReadOnlyDictionary<string, string>> _resources;
    private AppLanguage _currentLanguage = AppLanguage.ZhCn;

    public LocalizationService()
        : this(LoadEmbeddedResources())
    {
    }

    internal LocalizationService(Dictionary<AppLanguage, IReadOnlyDictionary<string, string>> resources)
    {
        _resources = resources;
    }

    public AppLanguage CurrentLanguage => _currentLanguage;

    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    public string this[string key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);
            if (TryGetValue(_currentLanguage, key, out var localized))
            {
                return localized;
            }

            return TryGetValue(AppLanguage.ZhCn, key, out var fallback) ? fallback : key;
        }
    }

    public LanguageChangeResult SetLanguage(AppLanguage language)
    {
        if (!AppLanguageExtensions.SupportedLanguages.Contains(language))
        {
            return LanguageChangeResult.InvalidCode(language.ToString());
        }

        if (_currentLanguage == language)
        {
            return LanguageChangeResult.Success(language);
        }

        _currentLanguage = language;
        LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(language));
        return LanguageChangeResult.Success(language);
    }

    public LanguageChangeResult SetLanguageCode(string code)
    {
        if (!AppLanguageExtensions.TryParseCode(code, out var language))
        {
            return LanguageChangeResult.InvalidCode(code);
        }

        return SetLanguage(language);
    }

    private bool TryGetValue(AppLanguage language, string key, out string value)
    {
        if (_resources.TryGetValue(language, out var resource) &&
            resource.TryGetValue(key, out var localized) &&
            localized.Length > 0)
        {
            value = localized;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static Dictionary<AppLanguage, IReadOnlyDictionary<string, string>> LoadEmbeddedResources()
    {
        var assembly = typeof(LocalizationService).Assembly;
        var resources = new Dictionary<AppLanguage, IReadOnlyDictionary<string, string>>();

        foreach (var language in AppLanguageExtensions.SupportedLanguages)
        {
            var resourceName = $"Luminalium.Core.Localization.Resources.{language.ToCode()}.json";
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Localization resource '{resourceName}' was not found.");
            resources[language] = LoadResource(stream, resourceName);
        }

        return resources;
    }

    private static IReadOnlyDictionary<string, string> LoadResource(Stream stream, string resourceName)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
                ?? throw new InvalidOperationException($"Localization resource '{resourceName}' is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Localization resource '{resourceName}' is malformed.", exception);
        }
    }
}
