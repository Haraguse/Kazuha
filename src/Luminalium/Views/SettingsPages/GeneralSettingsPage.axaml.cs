using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.general", FluentIcons.SettingsRegular)]
public partial class GeneralSettingsPage : MainSettingsPage
{
    public GeneralSettingsPage() : base(IAppHost.GetService<MainConfigHandler>()) => InitializeComponent();
}
