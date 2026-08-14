using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.Updater;

namespace Luminalium.App.ViewModels;

public sealed partial class UpdateDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string statusText = "Preparing update check...";

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
            StatusText = "Update check failed.";
            return ErrorText;
        }

        var preparation = result.Value!;
        ProgressValue = 100;
        IsErrorVisible = false;
        ErrorText = string.Empty;

        if (!preparation.Info.Available)
        {
            var status = UpdateOrchestrator.IsDevEnvironment(installDirectory)
                ? "Update checks are disabled in this development layout."
                : "Luminalium is up to date.";
            StatusText = status;
            return status;
        }

        var stagedStatus = $"Update {preparation.Info.Tag} downloaded and validated.";
        StatusText = stagedStatus;
        return stagedStatus;
    }

    private void OnProgress(UpdateProgress progress)
    {
        ProgressValue = Math.Clamp(progress.Percent, 0, 100);
        StatusText = progress.Status;
    }
}
