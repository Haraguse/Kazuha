using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Luminalium.App.ViewModels;

namespace Luminalium.App.Services;

public sealed class ShellNavigationService
{
    private FAFrame? _frame;

    public void AttachFrame(FAFrame frame)
    {
        _frame = frame;
        _frame.NavigationPageFactory = new ShellPageFactory();
    }

    public void Navigate(ShellPageViewModel page)
    {
        if (_frame is null)
        {
            return;
        }

        _frame.NavigateFromObject(page, new FAFrameNavigationOptions { IsNavigationStackEnabled = false });
    }
}
