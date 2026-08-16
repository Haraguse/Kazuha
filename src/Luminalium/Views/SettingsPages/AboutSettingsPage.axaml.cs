using Avalonia.Controls;
using Luminalium.Attributes;
using Luminalium.Icons;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.about", FluentIcons.InfoRegular)]
public partial class AboutSettingsPage : UserControl
{
    public AboutSettingsPage()
    {
        InitializeComponent();
    }
}