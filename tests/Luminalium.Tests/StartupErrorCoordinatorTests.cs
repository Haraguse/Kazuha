using Luminalium.App.Services;
using Luminalium.App.ViewModels;
using Xunit;

namespace Luminalium.Tests;

public sealed class StartupErrorCoordinatorTests
{
    [Fact]
    public async Task RetrySuccessReturnsRetried()
    {
        var dialog = new ScriptedRetryDialog(RetryCloseDialogResult.Retried);
        var coordinator = new StartupErrorCoordinator(dialog);
        var shell = new ShellViewModel(dialogService: dialog);

        var result = await coordinator.PresentAsync(shell, new InvalidOperationException("temporary"), () => Task.FromResult(true), () => { });

        Assert.Equal(RetryCloseDialogResult.Retried, result);
        Assert.Contains("temporary", dialog.Request!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetryFailureThenCloseCallsCloseOnce()
    {
        var closeCalls = 0;
        var dialog = new ScriptedRetryDialog(RetryCloseDialogResult.Closed);
        var coordinator = new StartupErrorCoordinator(dialog);
        var shell = new ShellViewModel(dialogService: dialog);

        var result = await coordinator.PresentAsync(shell, new InvalidOperationException("temporary"), () => Task.FromResult(false), () => closeCalls++);
        dialog.Request!.Close();
        dialog.Request.Close();

        Assert.Equal(RetryCloseDialogResult.Closed, result);
        Assert.Equal(1, closeCalls);
    }

    [Fact]
    public async Task OwnerNotReadyIsTypedAndDoesNotInvokeClose()
    {
        var dialog = new ScriptedRetryDialog(RetryCloseDialogResult.OwnerNotReady);
        var coordinator = new StartupErrorCoordinator(dialog);
        var shell = new ShellViewModel(dialogService: dialog);
        var closeCalls = 0;

        var result = await coordinator.PresentAsync(shell, new InvalidOperationException("temporary"), () => Task.FromResult(true), () => closeCalls++);

        Assert.Equal(RetryCloseDialogResult.OwnerNotReady, result);
        Assert.Equal(0, closeCalls);
    }

    [Fact]
    public async Task DuplicatePresentationReturnsAlreadyOpen()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialog = new BlockingRetryDialog(entered, release);
        var coordinator = new StartupErrorCoordinator(dialog);
        var shell = new ShellViewModel(dialogService: dialog);
        var first = coordinator.PresentAsync(shell, new InvalidOperationException("first"), () => Task.FromResult(true), () => { });
        await entered.Task;

        var second = await coordinator.PresentAsync(shell, new InvalidOperationException("second"), () => Task.FromResult(true), () => { });
        release.SetResult();
        await first;

        Assert.Equal(RetryCloseDialogResult.AlreadyOpen, second);
    }

    [Fact]
    public void SensitiveFailureTextDoesNotLeakRawDetails()
    {
        var shell = new ShellViewModel();
        var message = FailureTextSanitizer.CreateMessage(shell, new InvalidOperationException("password=secret PasswordHash=raw-config"));

        Assert.DoesNotContain("secret", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw-config", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", message, StringComparison.OrdinalIgnoreCase);
    }

    private class ScriptedRetryDialog(RetryCloseDialogResult result) : IDialogService
    {
        public RetryCloseDialogRequest? Request { get; private set; }

        public Task ShowAboutAsync(ShellViewModel shellViewModel) => Task.CompletedTask;
        public Task<string> ShowUpdateAsync(ShellViewModel shellViewModel, Luminalium.Updater.UpdateOrchestrator orchestrator, string installDirectory) => Task.FromResult(string.Empty);
        public Task<PasswordDialogResult> ShowPasswordAsync(ShellViewModel shellViewModel, PasswordDialogMode mode) => Task.FromResult(PasswordDialogResult.Cancelled);
        public virtual Task<RetryCloseDialogResult> ShowRetryCloseAsync(ShellViewModel shellViewModel, RetryCloseDialogRequest request)
        {
            Request = request;
            return Task.FromResult(result);
        }
    }

    private sealed class BlockingRetryDialog(TaskCompletionSource entered, TaskCompletionSource release) : ScriptedRetryDialog(RetryCloseDialogResult.Retried)
    {
        public override async Task<RetryCloseDialogResult> ShowRetryCloseAsync(ShellViewModel shellViewModel, RetryCloseDialogRequest request)
        {
            entered.SetResult();
            await release.Task;
            return RetryCloseDialogResult.Retried;
        }
    }
}
