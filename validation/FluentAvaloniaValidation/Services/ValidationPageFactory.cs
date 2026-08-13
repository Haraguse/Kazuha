using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using FluentAvaloniaValidation.Views;

namespace FluentAvaloniaValidation.Services;

/// <summary>
/// FAFrame navigation page factory: resolves a navigation tag to a native FluentAvalonia page.
/// Validates the FA view-model-first / object-based navigation contract.
/// </summary>
public sealed class ValidationPageFactory : IFANavigationPageFactory
{
    public Control? GetPage(Type srcType)
    {
        if (srcType == typeof(OverviewPage)) return new OverviewPage();
        if (srcType == typeof(InputPage)) return new InputPage();
        if (srcType == typeof(OverlayPage)) return new OverlayPage();
        return null; // fall back to default behavior
    }

    public Control? GetPageFromObject(object target)
    {
        return target switch
        {
            "overview" => new OverviewPage(),
            "input" => new InputPage(),
            "overlay" => new OverlayPage(),
            _ => null
        };
    }
}
