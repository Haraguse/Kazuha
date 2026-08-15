using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Luminalium.App.ViewModels;
using Luminalium.App.Views.Pages;

namespace Luminalium.App.Services;

public sealed class ShellPageFactory : IFANavigationPageFactory
{
    public Control? GetPage(Type srcType) => null;

    public Control? GetPageFromObject(object target)
    {
        if (target is not ShellPageViewModel viewModel)
        {
            return null;
        }

        return ShellPageMap.ResolvePageKind(viewModel.GetType()) switch
        {
            ShellPageKind.Overview => new OverviewPage { DataContext = viewModel },
            ShellPageKind.Settings => new SettingsPage { DataContext = viewModel },
            ShellPageKind.Onboarding => new OnboardingPage { DataContext = viewModel },
            ShellPageKind.Logs => new LogsPage { DataContext = viewModel },
            _ => null,
        };
    }
}
