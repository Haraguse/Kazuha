namespace Luminalium.App.ViewModels;

public sealed class AccentOptionViewModel(string key, string displayName)
{
    public string Key { get; } = key;

    public string DisplayName { get; } = displayName;

    public override string ToString() => DisplayName;
}
