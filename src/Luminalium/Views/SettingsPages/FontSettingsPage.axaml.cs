using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.fonts", FluentIcons.TextFontRegular)]
public partial class FontSettingsPage : MainSettingsPage
{
    public FontSettingsPage() : base(IAppHost.GetService<MainConfigHandler>()) => InitializeComponent();
}
