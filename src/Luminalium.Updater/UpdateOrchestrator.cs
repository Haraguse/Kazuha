using Luminalium.Core.Identity;

namespace Luminalium.Updater;

public sealed class UpdateOrchestrator
{
    private readonly IUpdateFeedClient _feedClient;
    private readonly UpdateDownloader _downloader;
    private readonly IUpdateValidator _validator;
    private readonly UpdateReplacementCoordinator _coordinator;

    public UpdateOrchestrator(
        IUpdateFeedClient? feedClient = null,
        UpdateDownloader? downloader = null,
        IUpdateValidator? validator = null,
        UpdateReplacementCoordinator? coordinator = null)
    {
        _feedClient = feedClient ?? new GitHubUpdateFeedClient();
        _downloader = downloader ?? new UpdateDownloader();
        _validator = validator ?? new UpdateValidator();
        _coordinator = coordinator ?? new UpdateReplacementCoordinator(_validator);
    }

    public event EventHandler<UpdateProgress>? ProgressChanged;

    public static bool IsDevEnvironment(string installDirectory) =>
        File.Exists(Path.Combine(installDirectory, ".dev")) ||
        !File.Exists(Path.Combine(installDirectory, "Luminalium.exe"));

    public async Task<UpdateOperationResult<UpdateInfo>> CheckAsync(
        string installDirectory,
        bool force,
        CancellationToken cancellationToken = default)
    {
        Report(new UpdateProgress(UpdateProgressStage.Checking, 0, "Checking for updates..."));
        var currentVersion = ReadCurrentVersion(installDirectory);

        if (IsDevEnvironment(installDirectory))
        {
            return UpdateOperation.Success(new UpdateInfo(
                Available: false,
                Tag: currentVersion,
                AssetName: null,
                DownloadUrl: null,
                Size: null,
                Changelog: "Development environment detected; update checks are disabled.",
                Forced: force));
        }

        return await _feedClient.CheckAsync(currentVersion, force, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UpdateOperationResult<UpdatePreparation>> PrepareAsync(
        string installDirectory,
        bool force,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var check = await CheckAsync(installDirectory, force, cancellationToken).ConfigureAwait(false);
        if (!check.IsSuccess)
        {
            return UpdateOperation.Failure<UpdatePreparation>(check.Error!);
        }

        if (check.Value is null || !check.Value.Available)
        {
            Report(progress, new UpdateProgress(UpdateProgressStage.Complete, 100, "Luminalium is up to date."));
            return UpdateOperation.Success(new UpdatePreparation(check.Value!, null));
        }

        var download = await _downloader.DownloadAsync(check.Value, Combine(progress), cancellationToken).ConfigureAwait(false);
        if (!download.IsSuccess)
        {
            return UpdateOperation.Failure<UpdatePreparation>(download.Error!);
        }

        Report(progress, new UpdateProgress(UpdateProgressStage.Validating, 90, "Validating update archive..."));
        var validation = await _validator.ValidateAsync(download.Value!.UpdateZipPath, cancellationToken).ConfigureAwait(false);
        if (!validation.IsSuccess)
        {
            return UpdateOperation.Failure<UpdatePreparation>(validation.Error!);
        }

        Report(progress, new UpdateProgress(UpdateProgressStage.Complete, 100, "Update staged and validated."));
        return UpdateOperation.Success(new UpdatePreparation(check.Value, download.Value));
    }

    public async Task<UpdateOperationResult<UpdateReplacementResult>> UpdateAsync(
        string installDirectory,
        bool force,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var preparation = await PrepareAsync(installDirectory, force, progress, cancellationToken).ConfigureAwait(false);
        if (!preparation.IsSuccess)
        {
            return UpdateOperation.Failure<UpdateReplacementResult>(preparation.Error!);
        }

        if (preparation.Value!.StagedUpdate is null)
        {
            return UpdateOperation.Failure<UpdateReplacementResult>(new UpdateError(
                UpdateErrorCode.UpToDate,
                "No update replacement was needed."));
        }

        return await _coordinator.ReplaceAsync(preparation.Value.StagedUpdate, installDirectory, Combine(progress), cancellationToken).ConfigureAwait(false);
    }

    private static string ReadCurrentVersion(string installDirectory)
    {
        var result = VersionMetadataReader.Load(Path.Combine(installDirectory, ProductIdentity.VersionMetadataFileName));
        return result.IsSuccess ? result.Metadata!.VersionName : string.Empty;
    }

    private Progress<UpdateProgress> Combine(IProgress<UpdateProgress>? progress) =>
        new Progress<UpdateProgress>(updateProgress => Report(progress, updateProgress));

    private void Report(UpdateProgress progress) => ProgressChanged?.Invoke(this, progress);

    private void Report(IProgress<UpdateProgress>? progress, UpdateProgress updateProgress)
    {
        progress?.Report(updateProgress);
        Report(updateProgress);
    }
}
