namespace Luminalium.Presentation;

public interface IPresentationHost
{
    PresentationHostKind HostKind { get; }

    Task<PresentationOperationResult> ConnectAsync(CancellationToken cancellationToken = default);

    Task<PresentationOperationResult<PresentationState>> GetStateAsync(CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> NavigateNextAsync(CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> NavigatePreviousAsync(CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> ApplyPenColorAsync(string colorHex, CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> SetPointerTypeAsync(PresentationPointerType pointerType, CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> ZoomAsync(double zoomFactor, CancellationToken cancellationToken = default);
}

public interface IOfficeComAdapter
{
    Task<PresentationOperationResult<SlideshowConnection>> ConnectPowerPointAsync(
        PresentationHostKind? preferredKind = null,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult<PresentationState>> GetStateAsync(
        SlideshowConnection connection,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> NavigateNextAsync(
        SlideshowConnection connection,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> NavigatePreviousAsync(
        SlideshowConnection connection,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> GotoSlideAsync(
        SlideshowConnection connection,
        int slideIndex,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> ApplyPenColorAsync(
        SlideshowConnection connection,
        string colorHex,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> ApplyPenColorViaPaletteAsync(
        SlideshowConnection connection,
        string colorHex,
        ISlideshowWindowAdapter windowAdapter,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> SetPointerTypeAsync(
        SlideshowConnection connection,
        PresentationPointerType pointerType,
        CancellationToken cancellationToken = default);
}

public interface ISlideshowWindowAdapter
{
    Task<PresentationOperationResult<SlideshowWindowInfo?>> FindSlideshowWindowAsync(
        PresentationHostKind? preferredKind = null,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult<PresentationWindowRect>> GetWindowRectAsync(
        nint hwnd,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> PostKeyAsync(
        nint hwnd,
        PresentationVirtualKey virtualKey,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> PostCtrlShortcutAsync(
        nint hwnd,
        PresentationVirtualKey virtualKey,
        CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> FocusAsync(
        nint hwnd,
        CancellationToken cancellationToken = default);
}

public interface IWpsAutomationAdapter
{
    bool IsConnected { get; }

    Task<PresentationOperationResult> ConnectAsync(CancellationToken cancellationToken = default);

    Task<PresentationOperationResult<WpsBridgePresentationState>> GetStateAsync(CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> NavigateNextAsync(CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> NavigatePreviousAsync(CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default);

    Task<PresentationOperationResult> ApplyPenColorAsync(string colorHex, CancellationToken cancellationToken = default);
}

public interface ICommandTimer
{
    PresentationOperationResult TryConsumePageTurn();
}
