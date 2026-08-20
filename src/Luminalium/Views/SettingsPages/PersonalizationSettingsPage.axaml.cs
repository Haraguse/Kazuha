using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.personalization", FluentIcons.PaintBrushRegular)]
public partial class PersonalizationSettingsPage : MainSettingsPage
{
    public PersonalizationSettingsPage() : base(IAppHost.GetService<MainConfigHandler>()) => InitializeComponent();
}
