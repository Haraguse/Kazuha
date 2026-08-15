using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

public sealed class FontFamilyOptionViewModel : ObservableObject
{
    public FontFamilyOptionViewModel(string family, ILocalizationService localization)
    {
        Family = family;
        DisplayName = family;
    }

    public string Family { get; }

    public string DisplayName { get; }

    public override string ToString() => DisplayName;
}
