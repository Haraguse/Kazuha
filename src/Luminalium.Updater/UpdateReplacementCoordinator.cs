using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace Luminalium.Updater;

public sealed class UpdateReplacementCoordinator
{
    public const string DefaultMutexName = "Luminalium_Updater";

    private readonly IUpdateValidator _validator;
    private readonly IUpdateFileSystem _fileSystem;
    private readonly string _mutexName;
    private readonly TimeSpan _processExitTimeout;

    public UpdateReplacementCoordinator(
        IUpdateValidator? validator = null,
        IUpdateFileSystem? fileSystem = null,
        string? mutexName = null,
        TimeSpan? processExitTimeout = null)
    {
        _validator = validator ?? new UpdateValidator();
        _fileSystem = fileSystem ?? new PhysicalUpdateFileSystem();
        _mutexName = mutexName ?? DefaultMutexName;
        _processExitTimeout = processExitTimeout ?? TimeSpan.FromSeconds(30);
    }

    public async Task<UpdateOperationResult<UpdateReplacementResult>> ReplaceAsync(
        StagedUpdate stagedUpdate,
        string installDirectory,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var mutex = new Mutex(false, _mutexName);
        var acquired = false;

        try
        {
            try
            {
                acquired = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }

            if (!acquired)
            {
                return UpdateOperation.Failure<UpdateReplacementResult>(new UpdateError(
                    UpdateErrorCode.MutexHeld,
                    "Another Luminalium update replacement is already running."));
            }

            progress?.Report(new UpdateProgress(UpdateProgressStage.Validating, 0, "Validating staged update..."));
            var validation = await _validator.ValidateAsync(stagedUpdate.UpdateZipPath, cancellationToken).ConfigureAwait(false);
            if (!validation.IsSuccess)
            {
                return UpdateOperation.Failure<UpdateReplacementResult>(validation.Error!);
            }

            var normalizedInstallDirectory = Path.GetFullPath(installDirectory);
            progress?.Report(new UpdateProgress(UpdateProgressStage.WaitingForExit, 5, "Waiting for Luminalium to exit..."));
            var waitResult = await WaitForRunningAppExitAsync(normalizedInstallDirectory, cancellationToken).ConfigureAwait(false);
            if (!waitResult.IsSuccess)
            {
                return UpdateOperation.Failure<UpdateReplacementResult>(waitResult.Error!);
            }

            var backupDirectory = CreateBackupDirectoryPath(normalizedInstallDirectory);
            var stageDirectory = Path.Combine(Path.GetTempPath(), "Luminalium-update-stage-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));

            try
            {
                ExtractValidatedPackage(stagedUpdate.UpdateZipPath, stageDirectory);
                _fileSystem.CreateDirectory(normalizedInstallDirectory);

                progress?.Report(new UpdateProgress(UpdateProgressStage.BackingUp, 20, "Backing up current installation..."));
                var backupResult = BackupInstall(normalizedInstallDirectory, backupDirectory);
                if (!backupResult.IsSuccess)
                {
                    return UpdateOperation.Failure<UpdateReplacementResult>(backupResult.Error!);
                }

                progress?.Report(new UpdateProgress(UpdateProgressStage.Replacing, 45, "Replacing installation files..."));
                return await ReplaceFilesAsync(stageDirectory, normalizedInstallDirectory, backupDirectory, progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Directory.Exists(stageDirectory))
                {
                    Directory.Delete(stageDirectory, recursive: true);
                }
            }
        }
        finally
        {
            if (acquired)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    private static string CreateBackupDirectoryPath(string installDirectory)
    {
        var trimmed = installDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parent = Path.GetDirectoryName(trimmed) ?? Path.GetTempPath();
        var name = Path.GetFileName(trimmed);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        return Path.Combine(parent, $"{name}.backup-{timestamp}");
    }

    private static void ExtractValidatedPackage(string zipPath, string stageDirectory)
    {
        Directory.CreateDirectory(stageDirectory);
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var destination = Path.GetFullPath(Path.Combine(stageDirectory, entry.FullName));
            if (!destination.StartsWith(Path.GetFullPath(stageDirectory), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The update archive contains an unsafe path entry.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, overwrite: true);
        }
    }

    private UpdateOperationResult BackupInstall(string installDirectory, string backupDirectory)
    {
        try
        {
            _fileSystem.CreateDirectory(backupDirectory);
            foreach (var sourcePath in _fileSystem.EnumerateFiles(installDirectory).Order(StringComparer.OrdinalIgnoreCase))
            {
                var relativePath = Path.GetRelativePath(installDirectory, sourcePath);
                var destinationPath = Path.Combine(backupDirectory, relativePath);
                _fileSystem.CopyFile(sourcePath, destinationPath, overwrite: true);
            }

            return UpdateOperationResult.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return UpdateOperationResult.Failure(new UpdateError(
                UpdateErrorCode.BackupFailed,
                "The current installation could not be backed up.",
                exception.Message,
                exception));
        }
    }

    private async Task<UpdateOperationResult<UpdateReplacementResult>> ReplaceFilesAsync(
        string stageDirectory,
        string installDirectory,
        string backupDirectory,
        IProgress<UpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        var copiedDestinations = new List<string>();
        var existingBackupRelatives = new HashSet<string>(
            _fileSystem.EnumerateFiles(backupDirectory).Select(path => Path.GetRelativePath(backupDirectory, path)),
            StringComparer.OrdinalIgnoreCase);

        try
        {
            var files = Directory.EnumerateFiles(stageDirectory, "*", SearchOption.AllDirectories)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (var index = 0; index < files.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sourcePath = files[index];
                var relativePath = Path.GetRelativePath(stageDirectory, sourcePath);
                var destinationPath = Path.Combine(installDirectory, relativePath);
                var copyResult = await CopyFileWithRetriesAsync(sourcePath, destinationPath, cancellationToken).ConfigureAwait(false);
                if (!copyResult.IsSuccess)
                {
                    throw new UpdateReplacementException(copyResult.Error!);
                }

                copiedDestinations.Add(destinationPath);
                var percent = 45 + (int)((index + 1) * 45d / Math.Max(1, files.Length));
                progress?.Report(new UpdateProgress(UpdateProgressStage.Replacing, percent, $"Copied {relativePath}."));
            }

            progress?.Report(new UpdateProgress(UpdateProgressStage.Complete, 100, "Update replacement complete."));
            return UpdateOperation.Success(new UpdateReplacementResult(installDirectory, backupDirectory, RolledBack: false, BackupRetained: true));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or UpdateReplacementException)
        {
            progress?.Report(new UpdateProgress(UpdateProgressStage.RollingBack, 90, "Replacement failed; restoring backup..."));
            var replacementError = exception is UpdateReplacementException typedException
                ? typedException.Error
                : new UpdateError(UpdateErrorCode.ReplacementFailed, "The update replacement failed.", exception.Message, exception);

            var rollback = RestoreBackup(installDirectory, backupDirectory, copiedDestinations, existingBackupRelatives);
            if (rollback.IsSuccess)
            {
                var result = new UpdateReplacementResult(installDirectory, backupDirectory, RolledBack: true, BackupRetained: true);
                return UpdateOperation.Failure(new UpdateError(
                    UpdateErrorCode.RollbackSucceeded,
                    "The update replacement failed and the original installation was restored.",
                    replacementError.Detail,
                    replacementError.Exception), result);
            }

            return UpdateOperation.Failure<UpdateReplacementResult>(new UpdateError(
                UpdateErrorCode.RollbackFailed,
                "The update replacement failed and rollback also failed.",
                rollback.Error!.Detail,
                rollback.Error.Exception));
        }
    }

    private async Task<UpdateOperationResult> CopyFileWithRetriesAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                _fileSystem.CopyFile(sourcePath, destinationPath, overwrite: true);
                return UpdateOperationResult.Success();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                lastException = exception;
                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return UpdateOperationResult.Failure(new UpdateError(
            lastException is IOException or UnauthorizedAccessException ? UpdateErrorCode.FileLocked : UpdateErrorCode.ReplacementFailed,
            "A replacement file could not be copied after retrying.",
            lastException?.Message,
            lastException));
    }

    private UpdateOperationResult RestoreBackup(
        string installDirectory,
        string backupDirectory,
        IEnumerable<string> copiedDestinations,
        HashSet<string> existingBackupRelatives)
    {
        try
        {
            foreach (var destinationPath in copiedDestinations.Reverse())
            {
                var relativePath = Path.GetRelativePath(installDirectory, destinationPath);
                if (!existingBackupRelatives.Contains(relativePath))
                {
                    _fileSystem.DeleteFile(destinationPath);
                }
            }

            foreach (var backupPath in _fileSystem.EnumerateFiles(backupDirectory).Order(StringComparer.OrdinalIgnoreCase))
            {
                var relativePath = Path.GetRelativePath(backupDirectory, backupPath);
                _fileSystem.CopyFile(backupPath, Path.Combine(installDirectory, relativePath), overwrite: true);
            }

            return UpdateOperationResult.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return UpdateOperationResult.Failure(new UpdateError(
                UpdateErrorCode.RollbackFailed,
                "Rollback restore failed.",
                exception.Message,
                exception));
        }
    }

    private async Task<UpdateOperationResult> WaitForRunningAppExitAsync(string installDirectory, CancellationToken cancellationToken)
    {
        var executablePath = Path.GetFullPath(Path.Combine(installDirectory, "Luminalium.exe"));
        var deadline = DateTimeOffset.UtcNow + _processExitTimeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var running = FindRunningLuminaliumProcess(executablePath);
            if (running is null)
            {
                return UpdateOperationResult.Success();
            }

            try
            {
                await running.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
            }
            finally
            {
                running.Dispose();
            }
        }

        return UpdateOperationResult.Failure(new UpdateError(
            UpdateErrorCode.ProcessExitTimeout,
            "Luminalium did not exit before the update replacement timeout.",
            executablePath));
    }

    private static Process? FindRunningLuminaliumProcess(string executablePath)
    {
        foreach (var process in Process.GetProcessesByName("Luminalium"))
        {
            try
            {
                if (process.Id == Environment.ProcessId)
                {
                    process.Dispose();
                    continue;
                }

                var modulePath = process.MainModule?.FileName;
                if (modulePath is not null && string.Equals(Path.GetFullPath(modulePath), executablePath, StringComparison.OrdinalIgnoreCase))
                {
                    return process;
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
            {
            }

            process.Dispose();
        }

        return null;
    }

    private sealed class UpdateReplacementException(UpdateError error) : Exception(error.Message)
    {
        public UpdateError Error { get; } = error;
    }
}
