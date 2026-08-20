using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.statusbar", FluentIcons.PanelBottomRegular)]
public partial class StatusBarSettingsPage : MainSettingsPage
{
    public StatusBarSettingsPage() : base(IAppHost.GetService<MainConfigHandler>()) => InitializeComponent();
}
