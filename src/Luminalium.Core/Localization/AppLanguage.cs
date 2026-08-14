namespace Luminalium.Core.Localization;

public enum AppLanguage
{
    ZhCn,
    ZhTw,
    YueHk,
    JaJp,
    EnUs,
    UgCn,
}

public static class AppLanguageExtensions
{
    public const string DefaultCode = "zh-CN";

    public static IReadOnlyList<AppLanguage> SupportedLanguages { get; } =
    [
        AppLanguage.ZhCn,
        AppLanguage.ZhTw,
        AppLanguage.YueHk,
        AppLanguage.JaJp,
        AppLanguage.EnUs,
        AppLanguage.UgCn,
    ];

    public static string ToCode(this AppLanguage language) => language switch
    {
        AppLanguage.ZhCn => "zh-CN",
        AppLanguage.ZhTw => "zh-TW",
        AppLanguage.YueHk => "yue-HK",
        AppLanguage.JaJp => "ja-JP",
        AppLanguage.EnUs => "en-US",
        AppLanguage.UgCn => "ug-CN",
        _ => DefaultCode,
    };

    public static bool TryParseCode(string? code, out AppLanguage language)
    {
        language = AppLanguage.ZhCn;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        foreach (var candidate in SupportedLanguages)
        {
            if (StringComparer.Ordinal.Equals(candidate.ToCode(), code))
            {
                language = candidate;
                return true;
            }
        }

        return false;
    }

    public static bool IsSupportedCode(string? code) => TryParseCode(code, out _);
}
