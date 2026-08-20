using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.notifications", FluentIcons.AlertRegular)]
public partial class NotificationSettingsPage : MainSettingsPage
{
    public NotificationSettingsPage() : base(IAppHost.GetService<MainConfigHandler>()) => InitializeComponent();
}
