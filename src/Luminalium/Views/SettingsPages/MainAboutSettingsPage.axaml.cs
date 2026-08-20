using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.about", FluentIcons.InfoRegular)]
public partial class MainAboutSettingsPage : MainSettingsPage
{
    public MainAboutSettingsPage() : base(IAppHost.GetService<MainConfigHandler>())
    {
        InitializeComponent();
        DataContext = new AboutSettingsPageViewModel(IAppHost.GetService<MainConfigHandler>());
    }
}
