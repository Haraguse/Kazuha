using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Luminalium.App.ViewModels;
using Luminalium.App.Views;
using Luminalium.Updater;

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
            shellViewModel.ReportError(ShellErrorKind.DialogAlreadyOpen, "A dialog is already open.");
            return;
        }

        if (_owner is null)
        {
            shellViewModel.ReportError(ShellErrorKind.DialogFailed, "Dialog owner is not ready.");
            return;
        }

        _dialogOpen = true;
        shellViewModel.ClearError();

        try
        {
            var dialog = new FAContentDialog
            {
                Title = "About Luminalium",
                Content = new AboutDialogContent(new AboutDialogViewModel(shellViewModel.VersionDisplay)),
                PrimaryButtonText = "Close",
                DefaultButton = FAContentDialogButton.Primary,
            };

            await dialog.ShowAsync(_owner);
        }
        catch (Exception exception)
        {
            shellViewModel.ReportError(ShellErrorKind.DialogFailed, $"Dialog failed: {exception.Message}");
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
            shellViewModel.ReportError(ShellErrorKind.DialogAlreadyOpen, "A dialog is already open.");
            return "A dialog is already open.";
        }

        if (_owner is null)
        {
            shellViewModel.ReportError(ShellErrorKind.DialogFailed, "Dialog owner is not ready.");
            return "Dialog owner is not ready.";
        }

        _dialogOpen = true;
        shellViewModel.ClearError();

        try
        {
            var viewModel = new UpdateDialogViewModel();
            var dialog = new FAContentDialog
            {
                Title = "Luminalium Updates",
                Content = new UpdateDialogContent(viewModel),
                PrimaryButtonText = "Close",
                DefaultButton = FAContentDialogButton.Primary,
            };

            var showTask = dialog.ShowAsync(_owner);
            var status = await viewModel.RunAsync(orchestrator, installDirectory).ConfigureAwait(true);
            await showTask.ConfigureAwait(true);
            return status;
        }
        catch (Exception exception)
        {
            var message = $"Update dialog failed: {exception.Message}";
            shellViewModel.ReportError(ShellErrorKind.DialogFailed, message);
            return message;
        }
        finally
        {
            _dialogOpen = false;
        }
    }
}
