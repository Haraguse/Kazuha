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

    public async Task<string> ShowUpdateAsync(ShellViewModel shellViewModel, UpdateOrchestrator orchestrator, string installDirectory)
    {
        if (_dialogOpen)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogAlreadyOpen, "Dialog.Error.AlreadyOpen");
            return shellViewModel.Localization["Dialog.Error.AlreadyOpen"];
        }

        if (_owner is null)
        {
            shellViewModel.ReportLocalizedError(ShellErrorKind.DialogFailed, "Dialog.Error.OwnerNotReady");
            return shellViewModel.Localization["Dialog.Error.OwnerNotReady"];
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
            return message;
        }
        finally
        {
            _dialogOpen = false;
        }
    }
}
