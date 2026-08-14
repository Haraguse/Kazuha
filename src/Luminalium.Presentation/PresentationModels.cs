namespace Luminalium.Presentation;

public enum PresentationPointerType
{
    Arrow,
    Pen,
    Highlighter,
    Eraser,
    Unknown,
}

public enum PresentationHostKind
{
    None,
    PowerPoint,
    Wps,
}

public enum PresentationErrorCode
{
    HostUnavailable,
    ComTimeout,
    BitnessMismatch,
    DisconnectedHost,
    SlideshowClosed,
    ProtectedViewRestricted,
    CommandRejected,
    Unsupported,
}

public enum PresentationNavigationCommand
{
    Next,
    Previous,
}

public enum PresentationVirtualKey
{
    Next = 0x22,
    Prior = 0x21,
    Left = 0x25,
    Right = 0x27,
    Down = 0x28,
    Up = 0x26,
    Return = 0x0D,
    Escape = 0x1B,
    Control = 0x11,
    A = 0x41,
    P = 0x50,
    I = 0x49,
    E = 0x45,
    M = 0x4D,
}

public sealed record PresentationState(
    int CurrentSlide,
    int SlideCount,
    bool IsSlideShow,
    PresentationPointerType PointerType,
    string PenColor,
    PresentationHostKind HostKind,
    bool IsProtectedView,
    bool IsReadOnly)
{
    public static PresentationState Empty { get; } = new(
        0,
        0,
        false,
        PresentationPointerType.Unknown,
        "#000000",
        PresentationHostKind.None,
        false,
        false);
}

public sealed record PresentationError(
    PresentationErrorCode Code,
    string Message,
    string? Detail = null,
    Exception? Exception = null);

public sealed record PresentationOperationResult(PresentationError? Error)
{
    public bool IsSuccess => Error is null;

    public static PresentationOperationResult Success() => new((PresentationError?)null);

    public static PresentationOperationResult Failure(PresentationError error) => new(error);
}

public sealed record PresentationOperationResult<T>(T Value, PresentationError? Error)
{
    public bool IsSuccess => Error is null;
}

public static class PresentationOperation
{
    public static PresentationOperationResult<T> Success<T>(T value) => new(value, null);

    public static PresentationOperationResult<T> Failure<T>(PresentationError error, T value = default!) => new(value, error);
}

public readonly record struct PresentationWindowRect(int Left, int Top, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

public sealed record SlideshowWindowInfo(
    nint Hwnd,
    PresentationHostKind HostKind,
    string ClassName,
    string ProcessName,
    string Title,
    PresentationWindowRect Rect);

public sealed record SlideshowConnection(
    object SlideshowWindow,
    nint Hwnd,
    PresentationHostKind HostKind,
    object? Application = null);

public sealed record PenColorPaletteCommand(
    string ExecuteMsoCommandId,
    PresentationVirtualKey ShortcutKey,
    int UpCount,
    int LeftCount,
    int DownCount,
    int RightCount,
    PresentationVirtualKey ConfirmKey);

public sealed record WpsBridgePresentationState(
    int CurrentSlide,
    int SlideCount,
    bool IsSlideShow,
    PresentationPointerType PointerType,
    string PenColor);
