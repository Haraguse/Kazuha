namespace Luminalium.App.Features;

public readonly record struct BuiltInFeatureId
{
    private static readonly Dictionary<string, BuiltInFeatureId> KnownIds =
        new Dictionary<string, BuiltInFeatureId>(StringComparer.Ordinal)
        {
            ["settings"] = new("settings"),
            ["onboarding"] = new("onboarding"),
            ["board"] = new("board"),
            ["timer"] = new("timer"),
            ["spotlight"] = new("spotlight"),
            ["app_launcher"] = new("app_launcher"),
            ["logs"] = new("logs"),
            ["status_bar"] = new("status_bar"),
        };

    private BuiltInFeatureId(string value)
    {
        Value = value;
    }

    public static BuiltInFeatureId Settings { get; } = KnownIds["settings"];
    public static BuiltInFeatureId Onboarding { get; } = KnownIds["onboarding"];
    public static BuiltInFeatureId Board { get; } = KnownIds["board"];
    public static BuiltInFeatureId Timer { get; } = KnownIds["timer"];
    public static BuiltInFeatureId Spotlight { get; } = KnownIds["spotlight"];
    public static BuiltInFeatureId AppLauncher { get; } = KnownIds["app_launcher"];
    public static BuiltInFeatureId Logs { get; } = KnownIds["logs"];
    public static BuiltInFeatureId StatusBar { get; } = KnownIds["status_bar"];

    public static IReadOnlyList<BuiltInFeatureId> All { get; } =
    [
        Settings,
        Onboarding,
        Board,
        Timer,
        Spotlight,
        AppLauncher,
        Logs,
        StatusBar,
    ];

    public string Value { get; }

    public static bool TryParseCanonical(string? value, out BuiltInFeatureId id)
    {
        if (value is not null && KnownIds.TryGetValue(value, out id))
        {
            return true;
        }

        id = default;
        return false;
    }

    public override string ToString() => Value ?? string.Empty;
}
