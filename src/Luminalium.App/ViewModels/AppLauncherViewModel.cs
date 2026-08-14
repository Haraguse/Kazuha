using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Localization;
using Luminalium.Core.Platform;

namespace Luminalium.App.ViewModels;

/// <summary>
/// App launcher view model: add/launch entries by name+path. Launching goes
/// through <see cref="IProcessLaunchService"/>; invalid paths produce a typed
/// NotFound result and NO process is started.
/// </summary>
public partial class AppLauncherViewModel : ObservableObject
{
    private readonly IProcessLaunchService _processLauncher;

    public AppLauncherViewModel(
        ILocalizationService? localization = null,
        IProcessLaunchService? processLauncher = null)
    {
        Localization = localization ?? new LocalizationService();
        _processLauncher = processLauncher ?? new ProcessLaunchService();
    }

    public ILocalizationService Localization { get; }

    public string WindowTitle => Localization["Launcher.Window.Title"];

    public ObservableCollection<LauncherEntry> Entries { get; } = [];

    [ObservableProperty]
    private LauncherEntry? _selectedEntry;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public bool AddEntry(string name, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalizedName = string.IsNullOrWhiteSpace(name)
            ? Path.GetFileNameWithoutExtension(path)
            : name;

        if (Entries.Any(entry => string.Equals(entry.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = string.Format(
                CultureInfo.InvariantCulture,
                Localization["Launcher.Status.Duplicate"],
                normalizedName);
            return false;
        }

        Entries.Add(new LauncherEntry(normalizedName, path));
        StatusText = string.Format(
            CultureInfo.InvariantCulture,
            Localization["Launcher.Status.Added"],
            normalizedName);
        return true;
    }

    public bool RemoveEntry(string path)
    {
        var entry = Entries.FirstOrDefault(candidate =>
            string.Equals(candidate.Path, path, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return false;
        }

        Entries.Remove(entry);
        if (ReferenceEquals(SelectedEntry, entry))
        {
            SelectedEntry = null;
        }

        StatusText = string.Format(
            CultureInfo.InvariantCulture,
            Localization["Launcher.Status.Removed"],
            entry.Name);
        return true;
    }

    [RelayCommand]
    private void Launch()
    {
        var entry = SelectedEntry;
        if (entry is null)
        {
            StatusText = Localization["Launcher.Status.SelectFirst"];
            return;
        }

        var result = _processLauncher.Launch(entry.Path);
        if (result.IsSuccess)
        {
            HasError = false;
            StatusText = string.Format(
                CultureInfo.InvariantCulture,
                Localization["Launcher.Status.Launched"],
                entry.Name);
            return;
        }

        HasError = true;
        StatusText = result.Error?.Code == PlatformOperationErrorCode.NotFound
            ? string.Format(
                CultureInfo.InvariantCulture,
                Localization["Launcher.Status.NotFound"],
                entry.Path)
            : string.Format(
                CultureInfo.InvariantCulture,
                Localization["Launcher.Status.LaunchFailed"],
                result.Error?.Message ?? entry.Path);
    }
}

/// <summary>
/// A single launcher entry: display name plus the absolute path to launch.
/// </summary>
public sealed record LauncherEntry(string Name, string Path);