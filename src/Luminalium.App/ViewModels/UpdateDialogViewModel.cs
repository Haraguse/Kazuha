using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Core.Localization;
using Luminalium.Updater;
using System.Globalization;

namespace Luminalium.App.ViewModels;

/// <summary>
/// Result of a completed update dialog run. <see cref="Status"/> is the text to
/// surface to the user; <see cref="RestartRequired"/> tells the shell to relaunch
/// Luminalium because the installation files were replaced.
/// </summary>
public sealed record UpdateDialogResult(string Status, bool RestartRequired);

public sealed partial class UpdateDialogViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;

    public UpdateDialogViewModel()
        : this(new LocalizationService())
    {
    }

    public UpdateDialogViewModel(ILocalizationService localization)
    {
        _localization = localization;
        statusText = _localization["UpdateDialog.Preparing"];
    }

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string errorText = string.Empty;

    [ObservableProperty]
    private int progressValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private bool isErrorVisible;

    public bool HasError => IsErrorVisible;

    public async Task<UpdateDialogResult> RunAsync(
        UpdateOrchestrator orchestrator,
        string installDirectory,
        CancellationToken cancellationToken = default)
    {
        var progress = new Progress<UpdateProgress>(OnProgress);
        var result = await orchestrator.PrepareAsync(installDirectory, force: false, progress, cancellationToken).ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            return Fail(result.Error!);
        }

        var preparation = result.Value!;
        IsErrorVisible = false;
        ErrorText = string.Empty;

        if (!preparation.Info.Available)
        {
            var status = UpdateOrchestrator.IsDevEnvironment(installDirectory)
                ? _localization["UpdateDialog.DevDisabled"]
                : _localization["UpdateDialog.UpToDate"];
            StatusText = status;
            return new UpdateDialogResult(status, RestartRequired: false);
        }

        var applied = await orchestrator.ApplyAsync(preparation, installDirectory, progress, cancellationToken).ConfigureAwait(true);
        if (!applied.IsSuccess)
        {
            return Fail(applied.Error!);
        }

        ProgressValue = 100;
        IsErrorVisible = false;
        ErrorText = string.Empty;
        var appliedStatus = string.Format(CultureInfo.InvariantCulture, _localization["UpdateDialog.Applied"], preparation.Info.Tag);
        StatusText = appliedStatus;
        return new UpdateDialogResult(appliedStatus, RestartRequired: true);
    }

    private UpdateDialogResult Fail(UpdateError error)
    {
        ProgressValue = 0;
        IsErrorVisible = true;
        ErrorText = error.Detail is { Length: > 0 }
            ? $"{error.Message} {error.Detail}"
            : error.Message;
        StatusText = _localization["UpdateDialog.Failed"];
        return new UpdateDialogResult(ErrorText, RestartRequired: false);
    }

    private void OnProgress(UpdateProgress progress)
    {
        ProgressValue = Math.Clamp(progress.Percent, 0, 100);
        StatusText = progress.Stage switch
        {
            UpdateProgressStage.Checking => _localization["UpdateDialog.Progress.Checking"],
            UpdateProgressStage.Downloading => _localization["UpdateDialog.Progress.Downloading"],
            UpdateProgressStage.Validating => _localization["UpdateDialog.Progress.Validating"],
            UpdateProgressStage.WaitingForExit => _localization["UpdateDialog.Progress.WaitingForExit"],
            UpdateProgressStage.BackingUp => _localization["UpdateDialog.Progress.BackingUp"],
            UpdateProgressStage.Replacing => _localization["UpdateDialog.Progress.Replacing"],
            UpdateProgressStage.RollingBack => _localization["UpdateDialog.Progress.RollingBack"],
            UpdateProgressStage.Complete => _localization["UpdateDialog.Progress.Complete"],
            UpdateProgressStage.Failed => _localization["UpdateDialog.Progress.Failed"],
            _ => progress.Status,
        };
    }
}
