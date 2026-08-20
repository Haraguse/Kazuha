using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;

namespace Luminalium.Localization.Settings;

public static class SettingsLocalization
{
    private static readonly ConcurrentDictionary<string, ResourceManager> Managers = new(StringComparer.Ordinal);

    public static string Get(string page, string key, CultureInfo? culture = null)
    {
        var manager = Managers.GetOrAdd(page, static name => new ResourceManager(
            $"Luminalium.Localization.Settings.{name}.Localization",
            typeof(SettingsLocalization).Assembly));
        return manager.GetString(key, culture ?? CultureInfo.CurrentUICulture) ?? key;
    }
}
