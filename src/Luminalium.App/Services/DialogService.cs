using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Luminalium.App.ViewModels;
using Luminalium.App.Views;
using Luminalium.Updater;
using System.Globalization;

namespace Luminalium.App.Services;

public sealed class DialogService : IDialogService
{
    private Window? _owner;
    private bool _dialogOpen;

    public void AttachOwner(Window owner) => _owner = owner;

    public async Task ShowAboutAsync(ShellViewModel shellViewModel)
    {
        if (_dialogOpen)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogAlreadyOpen, "Dialog.Error.AlreadyOpen");
            return;
        }

        if (_owner is null)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogFailed, "Dialog.Error.OwnerNotReady");
            return;
        }

        _dialogOpen = true;
        shellViewModel.ClearError();

        try
        {
            var dialog = new FAContentDialog
            {
                Title = shellViewModel.Localization["Dialog.About.Title"],
                Content = new AboutDialogContent(new AboutDialogViewModel(shellViewModel.VersionDisplay)),
                PrimaryButtonText = shellViewModel.Localization["Dialog.Button.Close"],
                DefaultButton = FAContentDialogButton.Primary,
            };

            await dialog.ShowAsync(_owner);
        }
        catch (Exception exception)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogFailed, "Dialog.Error.Failed", exception.Message);
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    public async Task<UpdateDialogResult> ShowUpdateAsync(ShellViewModel shellViewModel, UpdateOrchestrator orchestrator, string installDirectory)
    {
        if (_dialogOpen)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogAlreadyOpen, "Dialog.Error.AlreadyOpen");
            return new UpdateDialogResult(shellViewModel.Localization["Dialog.Error.AlreadyOpen"], RestartRequired: false);
        }

        if (_owner is null)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogFailed, "Dialog.Error.OwnerNotReady");
            return new UpdateDialogResult(shellViewModel.Localization["Dialog.Error.OwnerNotReady"], RestartRequired: false);
        }

        _dialogOpen = true;
        shellViewModel.ClearError();

        try
        {
            var viewModel = new UpdateDialogViewModel(shellViewModel.Localization);
            var dialog = new FAContentDialog
            {
                Title = shellViewModel.Localization["Dialog.Update.Title"],
                Content = new UpdateDialogContent(viewModel),
                PrimaryButtonText = shellViewModel.Localization["Dialog.Button.Close"],
                DefaultButton = FAContentDialogButton.Primary,
            };

            var showTask = dialog.ShowAsync(_owner);
            var status = await viewModel.RunAsync(orchestrator, installDirectory).ConfigureAwait(true);
            await showTask.ConfigureAwait(true);
            return status;
        }
        catch (Exception exception)
        {
            var message = string.Format(CultureInfo.InvariantCulture, shellViewModel.Localization["Dialog.Update.Error.Failed"], exception.Message);
            shellViewModel.ReportError(ShellErrorKind.DialogFailed, message);
            return new UpdateDialogResult(message, RestartRequired: false);
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    public async Task<PasswordDialogResult> ShowPasswordAsync(ShellViewModel shellViewModel, PasswordDialogMode mode)
    {
        if (_dialogOpen)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogAlreadyOpen, "Dialog.Error.AlreadyOpen");
            return PasswordDialogResult.Cancelled;
        }

        if (_owner is null)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogFailed, "Dialog.Error.OwnerNotReady");
            return PasswordDialogResult.Cancelled;
        }

        _dialogOpen = true;
        shellViewModel.ClearError();

        try
        {
            var viewModel = new PasswordDialogViewModel(shellViewModel.Localization, mode);
            var dialog = new FAContentDialog
            {
                Title = viewModel.Title,
                Content = new PasswordDialogContent(viewModel),
                PrimaryButtonText = viewModel.PrimaryButtonText,
                CloseButtonText = viewModel.CloseButtonText,
                DefaultButton = FAContentDialogButton.Primary,
            };

            var result = await dialog.ShowAsync(_owner).ConfigureAwait(true);
            return result == FAContentDialogResult.Primary
                ? viewModel.Submit()
                : PasswordDialogResult.Cancelled;
        }
        catch (Exception exception)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogFailed, "Dialog.Error.Failed", exception.Message);
            return PasswordDialogResult.Cancelled;
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    public async Task<RetryCloseDialogResult> ShowRetryCloseAsync(
        ShellViewModel shellViewModel,
        RetryCloseDialogRequest request)
    {
        if (_dialogOpen)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogAlreadyOpen, "Dialog.Error.AlreadyOpen");
            return RetryCloseDialogResult.AlreadyOpen;
        }

        if (_owner is null || !_owner.IsVisible)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogFailed, "Dialog.Error.OwnerNotReady");
            return RetryCloseDialogResult.OwnerNotReady;
        }

        _dialogOpen = true;
        shellViewModel.ClearError();
        var closeCalled = false;
        void CloseOnce()
        {
            if (!closeCalled)
            {
                closeCalled = true;
                request.Close();
            }
        }

        try
        {
            while (true)
            {
                var viewModel = new RetryCloseDialogViewModel(request.Message, request.RetryAsync, CloseOnce);
                var dialog = new FAContentDialog
                {
                    Title = request.Title,
                    Content = new RetryCloseDialogContent(viewModel),
                    PrimaryButtonText = shellViewModel.Localization["Dialog.Button.Retry"],
                    CloseButtonText = shellViewModel.Localization["Dialog.Button.Close"],
                    DefaultButton = FAContentDialogButton.Primary,
                };
                var result = await dialog.ShowAsync(_owner).ConfigureAwait(true);
                if (result != FAContentDialogResult.Primary)
                {
                    CloseOnce();
                    return RetryCloseDialogResult.Closed;
                }

                if (await request.RetryAsync().ConfigureAwait(true))
                {
                    return RetryCloseDialogResult.Retried;
                }
            }
        }
        catch (Exception exception)
        {
            shellViewModel.ReportError(ShellErrorKind.DialogFailed, FailureTextSanitizer.CreateMessage(shellViewModel, exception));
            return RetryCloseDialogResult.Failed;
        }
        finally
        {
            _dialogOpen = false;
        }
    }
}
