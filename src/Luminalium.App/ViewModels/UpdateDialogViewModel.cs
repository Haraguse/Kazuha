using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Core.Localization;
using Luminalium.Updater;
using System.Globalization;

namespace Luminalium.App.ViewModels;

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

    public async Task<string> RunAsync(
        UpdateOrchestrator orchestrator,
        string installDirectory,
        CancellationToken cancellationToken = default)
    {
        var progress = new Progress<UpdateProgress>(OnProgress);
        var result = await orchestrator.PrepareAsync(installDirectory, force: false, progress, cancellationToken).ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            ProgressValue = 0;
            IsErrorVisible = true;
            ErrorText = result.Error!.Detail is { Length: > 0 }
                ? $"{result.Error.Message} {result.Error.Detail}"
                : result.Error.Message;
            StatusText = _localization["UpdateDialog.Failed"];
            return ErrorText;
        }

        var preparation = result.Value!;
        ProgressValue = 100;
        IsErrorVisible = false;
        ErrorText = string.Empty;

        if (!preparation.Info.Available)
        {
            var status = UpdateOrchestrator.IsDevEnvironment(installDirectory)
                ? _localization["UpdateDialog.DevDisabled"]
                : _localization["UpdateDialog.UpToDate"];
            StatusText = status;
            return status;
        }

        var stagedStatus = string.Format(CultureInfo.InvariantCulture, _localization["UpdateDialog.Downloaded"], preparation.Info.Tag);
        StatusText = stagedStatus;
        return stagedStatus;
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
