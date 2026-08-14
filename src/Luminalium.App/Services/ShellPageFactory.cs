using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Luminalium.App.ViewModels;
using Luminalium.App.Views.Pages;

namespace Luminalium.App.Services;

public sealed class ShellPageFactory : IFANavigationPageFactory
{
    public Control? GetPage(Type srcType) => null;

    public Control? GetPageFromObject(object target) => target switch
    {
        OverviewViewModel viewModel => new OverviewPage { DataContext = viewModel },
        SettingsViewModel viewModel => new SettingsPage { DataContext = viewModel },
        PluginPageViewModel viewModel => new PluginPage { DataContext = viewModel },
        _ => null,
    };
}
