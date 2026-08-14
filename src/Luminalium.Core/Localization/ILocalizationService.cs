namespace Luminalium.Core.Localization;

public interface ILocalizationService
{
    string this[string key] { get; }

    AppLanguage CurrentLanguage { get; }

    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    LanguageChangeResult SetLanguage(AppLanguage language);

    LanguageChangeResult SetLanguageCode(string code);
}

public sealed class LanguageChangedEventArgs(AppLanguage language) : EventArgs
{
    public AppLanguage Language { get; } = language;
}

public sealed record LanguageChangeResult(bool IsSuccess, AppLanguage Language, string Code, string? Error)
{
    public static LanguageChangeResult Success(AppLanguage language) =>
        new(true, language, language.ToCode(), null);

    public static LanguageChangeResult InvalidCode(string code) =>
        new(false, AppLanguage.ZhCn, code, $"Language code '{code}' is not supported.");
}
