using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.linkage", FluentIcons.ArrowSyncRegular)]
public partial class LinkageSettingsPage : MainSettingsPage
{
    public LinkageSettingsPage() : base(IAppHost.GetService<MainConfigHandler>()) => InitializeComponent();
}
