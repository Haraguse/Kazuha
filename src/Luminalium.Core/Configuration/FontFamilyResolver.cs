namespace Luminalium.Core.Configuration;

public static class FontFamilyResolver
{
    public const int MaximumFamilyNameLength = 128;

    public const string DefaultFontFamilyStack =
        "Segoe UI, Microsoft YaHei UI, Microsoft JhengHei UI, Yu Gothic UI, Meiryo UI, PingFang SC, PingFang TC, Malgun Gothic, sans-serif";

    public static IReadOnlyList<string> DefaultFontFamilies { get; } =
    [
        "Segoe UI",
        "Microsoft YaHei UI",
        "Microsoft JhengHei UI",
        "Yu Gothic UI",
        "Meiryo UI",
        "PingFang SC",
        "PingFang TC",
        "Malgun Gothic",
    ];

    public static string Resolve(string? configuredFamily) =>
        Resolve(configuredFamily, DefaultFontFamilies);

    public static string Resolve(
        string? configuredFamily,
        IEnumerable<string> knownFamilies)
    {
        ArgumentNullException.ThrowIfNull(knownFamilies);

        var family = configuredFamily?.Trim();
        if (!IsSafeFamilyName(family))
        {
            return DefaultFontFamilyStack;
        }

        var isKnown = knownFamilies.Any(knownFamily =>
        {
            var normalizedKnownFamily = knownFamily?.Trim();
            return IsSafeFamilyName(normalizedKnownFamily) &&
                string.Equals(
                    normalizedKnownFamily,
                    family,
                    StringComparison.OrdinalIgnoreCase);
        });
        if (!isKnown)
        {
            return DefaultFontFamilyStack;
        }

        if (DefaultFontFamilies.Any(
                defaultFamily => string.Equals(
                    defaultFamily,
                    family,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return DefaultFontFamilyStack;
        }

        return $"{family}, {DefaultFontFamilyStack}";
    }

    private static bool IsSafeFamilyName(string? family) =>
        !string.IsNullOrEmpty(family) &&
        family.Length <= MaximumFamilyNameLength &&
        family.All(character =>
            !char.IsControl(character) &&
            character is not ',' and not ';' and not '"' and not '\'' and not '\\');
}
