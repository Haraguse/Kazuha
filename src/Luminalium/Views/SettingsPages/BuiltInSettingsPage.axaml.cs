using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.built-in", FluentIcons.AppsRegular)]
public partial class BuiltInSettingsPage : MainSettingsPage
{
    public BuiltInSettingsPage() : base(IAppHost.GetService<MainConfigHandler>()) => InitializeComponent();
}
