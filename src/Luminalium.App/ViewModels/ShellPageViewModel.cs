using CommunityToolkit.Mvvm.ComponentModel;

namespace Luminalium.App.ViewModels;

public abstract class ShellPageViewModel(string navigationKey, string title) : ObservableObject
{
    public string NavigationKey { get; } = navigationKey;

    public string Title { get; } = title;
}
