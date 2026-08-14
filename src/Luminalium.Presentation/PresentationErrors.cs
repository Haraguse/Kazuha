using System.Runtime.InteropServices;
using System.Reflection;
using Luminalium.Core.Platform;

namespace Luminalium.Presentation;

public static class PresentationErrors
{
    public static PresentationError Create(
        PresentationErrorCode code,
        string message,
        string? detail = null,
        Exception? exception = null) =>
        new(code, message, detail, exception);

    public static PresentationError HostUnavailable(string message, string? detail = null, Exception? exception = null) =>
        Create(PresentationErrorCode.HostUnavailable, message, detail, exception);

    public static PresentationError SlideshowClosed(string message = "The slideshow is closed.", string? detail = null, Exception? exception = null) =>
        Create(PresentationErrorCode.SlideshowClosed, message, detail, exception);

    public static PresentationError ProtectedViewRestricted(string message = "The presentation is restricted by Protected View or read-only state.", string? detail = null) =>
        Create(PresentationErrorCode.ProtectedViewRestricted, message, detail);

    public static PresentationError CommandRejected(string message, string? detail = null, Exception? exception = null) =>
        Create(PresentationErrorCode.CommandRejected, message, detail, exception);

    public static PresentationError Unsupported(string message, string? detail = null) =>
        Create(PresentationErrorCode.Unsupported, message, detail);

    public static PresentationError FromPlatform(PlatformOperationError error) =>
        error.Code switch
        {
            PlatformOperationErrorCode.Unavailable => HostUnavailable(error.Message, error.Detail),
            PlatformOperationErrorCode.NotFound => SlideshowClosed(error.Message, error.Detail),
            _ => CommandRejected(error.Message, error.Detail),
        };

    public static PresentationError FromException(Exception exception, string context)
    {
        if (exception is TargetInvocationException targetInvocationException && targetInvocationException.InnerException is not null)
        {
            return FromException(targetInvocationException.InnerException, context);
        }

        if (exception is OperationCanceledException)
        {
            return Create(PresentationErrorCode.ComTimeout, $"Timed out while {context}.", exception.Message, exception);
        }

        if (exception is COMException comException)
        {
            return FromComException(comException, context);
        }

        if (exception is TypeLoadException or BadImageFormatException or TypeInitializationException)
        {
            return Create(PresentationErrorCode.BitnessMismatch, $"Office COM automation could not be loaded while {context}.", exception.Message, exception);
        }

        if (exception is ObjectDisposedException or InvalidOperationException)
        {
            return Create(PresentationErrorCode.DisconnectedHost, $"The presentation host disconnected while {context}.", exception.Message, exception);
        }

        return CommandRejected($"The presentation command was rejected while {context}.", exception.Message, exception);
    }

    public static PresentationError FromComException(COMException exception, string context)
    {
        var hresult = unchecked((uint)exception.HResult);
        return hresult switch
        {
            0x80040154 or 0x800401E3 or 0x800401F0 => HostUnavailable(
                $"PowerPoint COM automation is unavailable while {context}.",
                $"HRESULT 0x{hresult:X8}: {exception.Message}",
                exception),
            0x80010001 or 0x8001010A => Create(
                PresentationErrorCode.ComTimeout,
                $"PowerPoint COM automation timed out while {context}.",
                $"HRESULT 0x{hresult:X8}: {exception.Message}",
                exception),
            0x800706BA or 0x80010108 => Create(
                PresentationErrorCode.DisconnectedHost,
                $"PowerPoint disconnected while {context}.",
                $"HRESULT 0x{hresult:X8}: {exception.Message}",
                exception),
            0x8007000B => Create(
                PresentationErrorCode.BitnessMismatch,
                $"PowerPoint COM automation bitness does not match the current process while {context}.",
                $"HRESULT 0x{hresult:X8}: {exception.Message}",
                exception),
            _ => CommandRejected(
                $"PowerPoint COM automation rejected the request while {context}.",
                $"HRESULT 0x{hresult:X8}: {exception.Message}",
                exception),
        };
    }
}
