using System.Collections.ObjectModel;
using Luminalium.Attributes;

namespace Luminalium.Services;

public static class SettingsPagesRegistryService
{
    public static ObservableCollection<SettingsPageInfo> Items { get; } = [];
    public static ObservableCollection<SettingsPageInfo> FooterItems { get; } = [];
}