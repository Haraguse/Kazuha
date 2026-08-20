using Luminalium.Attributes;
using Luminalium.Icons;
using Luminalium.Services.Config;
using Luminalium.Shared;

namespace Luminalium.Views.SettingsPages;

[SettingsPageInfo("settings.storage", FluentIcons.FolderOpenRegular)]
public partial class StorageSettingsPage : MainSettingsPage
{
    public StorageSettingsPage() : base(IAppHost.GetService<MainConfigHandler>())
    {
        InitializeComponent();
        DataContext = new StorageSettingsPageViewModel(IAppHost.GetService<MainConfigHandler>());
    }
}
