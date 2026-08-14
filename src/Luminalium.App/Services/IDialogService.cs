using Luminalium.App.ViewModels;
using Luminalium.Updater;

namespace Luminalium.App.Services;

public interface IDialogService
{
    Task ShowAboutAsync(ShellViewModel shellViewModel);

    Task<string> ShowUpdateAsync(ShellViewModel shellViewModel, UpdateOrchestrator orchestrator, string installDirectory);
}
