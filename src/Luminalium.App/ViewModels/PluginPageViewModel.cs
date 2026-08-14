namespace Luminalium.App.ViewModels;

using Luminalium.Core.Localization;

public sealed class PluginPageViewModel : ShellPageViewModel
{
    private readonly ILocalizationService _localization;

    public PluginPageViewModel(PluginEntryViewModel plugin, ILocalizationService localization)
        : base($"plugin:{plugin.Id}", "PluginPage.Title", localization)
    {
        _localization = localization;
        Plugin = plugin;
    }

    public PluginEntryViewModel Plugin { get; }

    public override string Title => Plugin.DisplayName;

    public string TypeLabel => _localization["PluginPage.Type"];

    public string IconKeyLabel => _localization["PluginPage.IconKey"];

    public string VersionLabel => _localization["PluginPage.Version"];

    public string DescriptionLabel => _localization["PluginPage.Description"];

    public string PlaceholderTitle => _localization["PluginPage.Placeholder.Title"];

    public string PlaceholderNotice => _localization["PluginPage.Placeholder.Notice"];

    protected override void RefreshLocalizedText()
    {
        base.RefreshLocalizedText();
        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(IconKeyLabel));
        OnPropertyChanged(nameof(VersionLabel));
        OnPropertyChanged(nameof(DescriptionLabel));
        OnPropertyChanged(nameof(PlaceholderTitle));
        OnPropertyChanged(nameof(PlaceholderNotice));
    }
}
