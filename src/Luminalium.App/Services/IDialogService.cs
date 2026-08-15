using Luminalium.App.ViewModels;
using Luminalium.Updater;

namespace Luminalium.App.Services;

public interface IDialogService
{
    Task ShowAboutAsync(ShellViewModel shellViewModel);

    Task<UpdateDialogResult> ShowUpdateAsync(ShellViewModel shellViewModel, UpdateOrchestrator orchestrator, string installDirectory);

    Task<PasswordDialogResult> ShowPasswordAsync(ShellViewModel shellViewModel, PasswordDialogMode mode);

    Task<RetryCloseDialogResult> ShowRetryCloseAsync(
        ShellViewModel shellViewModel,
        RetryCloseDialogRequest request);
}
