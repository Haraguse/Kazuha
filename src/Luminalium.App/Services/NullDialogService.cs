using Luminalium.App.ViewModels;
using Luminalium.Updater;

namespace Luminalium.App.Services;

public sealed class NullDialogService : IDialogService
{
    public static NullDialogService Instance { get; } = new();

    private NullDialogService()
    {
    }

    public Task ShowAboutAsync(ShellViewModel shellViewModel) => Task.CompletedTask;

    public Task<UpdateDialogResult> ShowUpdateAsync(ShellViewModel shellViewModel, UpdateOrchestrator orchestrator, string installDirectory) =>
        Task.FromResult(new UpdateDialogResult(shellViewModel.Localization["Dialog.Update.Error.ServiceUnavailable"], RestartRequired: false));

    public Task<PasswordDialogResult> ShowPasswordAsync(ShellViewModel shellViewModel, PasswordDialogMode mode) =>
        Task.FromResult(PasswordDialogResult.Cancelled);

    public Task<RetryCloseDialogResult> ShowRetryCloseAsync(
        ShellViewModel shellViewModel,
        RetryCloseDialogRequest request) =>
        Task.FromResult(RetryCloseDialogResult.OwnerNotReady);
}
