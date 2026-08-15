using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

/// <summary>
/// Status bar view model: shows presentation status (slide N / M) and media
/// status (title - artist) from injectable sources. Empty states render
/// localized placeholders and never throw.
/// </summary>
public partial class StatusBarViewModel : ObservableObject
{
    private readonly IPresentationStatusSource _presentationStatus;
    private readonly IMediaStatusSource _mediaStatus;

    public StatusBarViewModel(
        ILocalizationService? localization = null,
        IPresentationStatusSource? presentationStatus = null,
        IMediaStatusSource? mediaStatus = null)
    {
        Localization = localization ?? new LocalizationService();
        _presentationStatus = presentationStatus ?? new PresentationStatusSource(
            new Luminalium.Presentation.PresentationMonitor([]));
        _mediaStatus = mediaStatus ?? new MediaStatusSource(new UnavailableSmtcService());
    }

    public ILocalizationService Localization { get; }

    public string WindowTitle => Localization["StatusBar.Window.Title"];

    [ObservableProperty]
    private string _presentationText = string.Empty;

    [ObservableProperty]
    private string _mediaText = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var presentation = await _presentationStatus.GetStatusAsync();
        PresentationText = presentation.IsSlideShow && presentation.SlideCount > 0
            ? string.Format(
                CultureInfo.InvariantCulture,
                Localization["StatusBar.Status.Presentation"],
                presentation.CurrentSlide,
                presentation.SlideCount)
            : Localization["StatusBar.Status.NoPresentation"];

        var media = await _mediaStatus.GetStatusAsync();
        MediaText = string.IsNullOrWhiteSpace(media.Title)
            ? Localization["StatusBar.Status.NoMedia"]
            : string.Format(
                CultureInfo.InvariantCulture,
                Localization["StatusBar.Status.Media"],
                media.Title,
                media.Artist);

        StatusText = string.Empty;
    }
}

/// <summary>
/// Safe default SMTC service used when no real adapter is wired yet: always
/// reports an empty session so the status bar shows its "no media" placeholder.
/// </summary>
internal sealed class UnavailableSmtcService : Luminalium.Core.Media.ISmtcService
{
    public Task<Luminalium.Core.Platform.PlatformOperationResult<Luminalium.Core.Media.SmtcSessionSnapshot>> TryGetSnapshotAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Luminalium.Core.Platform.PlatformOperation.Success(Luminalium.Core.Media.SmtcSessionSnapshot.Empty));

    public Task<Luminalium.Core.Platform.PlatformOperationResult<Luminalium.Core.Media.SmtcSessionSnapshot>> GetCurrentSessionAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Luminalium.Core.Platform.PlatformOperation.Success(Luminalium.Core.Media.SmtcSessionSnapshot.Empty));
}
