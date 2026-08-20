using Avalonia.Controls;
using Luminalium.Services.Config;

namespace Luminalium.Views.SettingsPages;

public abstract class MainSettingsPage : UserControl
{
    protected MainSettingsPage(MainConfigHandler handler)
    {
        DataContext = handler.Data;
        Classes.Add("settings-page");
    }
}
