using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.pen", FluentIcons.EditRegular)]
public partial class PenSettingsPage : MainSettingsPage
{
    public PenSettingsPage() : base(IAppHost.GetService<MainConfigHandler>()) => InitializeComponent();
}
