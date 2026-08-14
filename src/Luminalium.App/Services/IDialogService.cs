using Luminalium.App.ViewModels;

namespace Luminalium.App.Services;

public interface IDialogService
{
    Task ShowAboutAsync(ShellViewModel shellViewModel);
}
