using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.window", FluentIcons.WindowRegular)]
public partial class WindowSettingsPage : MainSettingsPage
{
    public WindowSettingsPage() : base(IAppHost.GetService<MainConfigHandler>()) => InitializeComponent();
}
