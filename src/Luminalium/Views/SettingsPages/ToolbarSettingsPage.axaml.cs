using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.toolbar", FluentIcons.ToolboxRegular)]
public partial class ToolbarSettingsPage : MainSettingsPage
{
    public ToolbarSettingsPage() : base(IAppHost.GetService<MainConfigHandler>()) => InitializeComponent();
}
