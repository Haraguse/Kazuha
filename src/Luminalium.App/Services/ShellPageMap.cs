using Luminalium.App.ViewModels;

namespace Luminalium.App.Services;

/// <summary>
/// Identifies the native shell page rendered for a built-in page ViewModel.
/// Overview, settings, onboarding, and logs are native application pages and
/// must never be carried through the external plugin page adapter.
/// </summary>
public enum ShellPageKind
{
    Overview,
    Settings,
    Onboarding,
    Logs,
}

/// <summary>
/// Typed mapping from native shell page ViewModel types to their page kind.
/// Kept free of Avalonia controls and plugin page types so the mapping is
/// unit-testable without instantiating UI or XAML resources.
/// </summary>
public static class ShellPageMap
{
    public static ShellPageKind? ResolvePageKind(Type viewModelType)
    {
        if (viewModelType == typeof(OverviewViewModel))
        {
            return ShellPageKind.Overview;
        }

        if (viewModelType == typeof(SettingsViewModel))
        {
            return ShellPageKind.Settings;
        }

        if (viewModelType == typeof(OnboardingViewModel))
        {
            return ShellPageKind.Onboarding;
        }

        if (viewModelType == typeof(LogsViewModel))
        {
            return ShellPageKind.Logs;
        }

        return null;
    }
}
