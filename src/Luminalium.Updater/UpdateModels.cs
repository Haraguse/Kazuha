namespace Luminalium.Updater;

public sealed record UpdateMirror(string Name, string ApiBaseUrl);

public sealed record UpdateInfo(
    bool Available,
    string Tag,
    string? AssetName,
    Uri? DownloadUrl,
    long? Size,
    string Changelog,
    bool Forced);

public enum UpdateProgressStage
{
    Checking,
    Downloading,
    Validating,
    WaitingForExit,
    BackingUp,
    Replacing,
    RollingBack,
    Complete,
    Failed,
}

public sealed record UpdateProgress(
    UpdateProgressStage Stage,
    int Percent,
    string Status,
    long? BytesReceived = null,
    long? TotalBytes = null);

public sealed record StagedUpdate(
    string Tag,
    string UpdateZipPath,
    string CacheDirectory,
    long Size);

public sealed record ValidatedUpdatePackage(
    string ZipPath,
    IReadOnlyList<string> Entries);

public sealed record UpdatePreparation(UpdateInfo Info, StagedUpdate? StagedUpdate);

public sealed record UpdateReplacementResult(
    string InstallDirectory,
    string BackupDirectory,
    bool RolledBack,
    bool BackupRetained);
