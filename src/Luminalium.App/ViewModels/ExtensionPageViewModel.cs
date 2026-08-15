namespace Luminalium.App.ViewModels;

using Luminalium.Core.Localization;

/// <summary>
/// Shell page model for a genuine external extension's detail surface. Built-in
/// features never route through this carrier; it exists only for the external
/// plugin compatibility boundary.
/// </summary>
public sealed class ExtensionPageViewModel : ShellPageViewModel
{
    private readonly ILocalizationService _localization;

    public ExtensionPageViewModel(ExtensionEntryViewModel extension, ILocalizationService localization)
        : base($"plugin:{extension.Id}", "PluginPage.Title", localization)
    {
        _localization = localization;
        Extension = extension;
    }

    public ExtensionEntryViewModel Extension { get; }

    public override string Title => Extension.DisplayName;

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
