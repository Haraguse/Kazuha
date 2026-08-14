using System.ComponentModel;
using System.Runtime.InteropServices;
using Luminalium.Core.Platform;

namespace Luminalium.Platform.Windows.Platform;

public sealed class WindowStyleService : IWindowStyleService
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmColorNone = unchecked((int)0xFFFFFFFE);
    private const int DwmWindowCornerPreferenceRound = 0;
    private const int DwmWindowCornerPreferenceDoNotRound = 2;

    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsPopup = 0x80000000L;
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const long WsSysMenu = 0x00080000L;
    private const long WsExAppWindow = 0x00040000L;
    private const long WsExToolWindow = 0x00000080L;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;

    private const int EInvalidArg = unchecked((int)0x80070057);
    private const int DwmECompositionDisabled = unchecked((int)0x80263001);

    public PlatformOperationResult ApplyBorderColorNone(IntPtr hwnd)
    {
        var handleValidation = ValidateHandle(hwnd);
        if (!handleValidation.IsSuccess)
        {
            return handleValidation;
        }

        var value = DwmColorNone;
        return SetDwmAttribute(hwnd, DwmwaBorderColor, ref value, "DWM border color-none");
    }

    public PlatformOperationResult ApplyDarkTitleBar(IntPtr hwnd, bool enabled)
    {
        var handleValidation = ValidateHandle(hwnd);
        if (!handleValidation.IsSuccess)
        {
            return handleValidation;
        }

        var value = enabled ? 1 : 0;
        return SetDwmAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref value, "DWM immersive dark mode");
    }

    public PlatformOperationResult ApplyCornerPreference(IntPtr hwnd, WindowCornerPreference preference)
    {
        var handleValidation = ValidateHandle(hwnd);
        if (!handleValidation.IsSuccess)
        {
            return handleValidation;
        }

        var value = preference switch
        {
            WindowCornerPreference.Round => DwmWindowCornerPreferenceRound,
            WindowCornerPreference.DoNotRound => DwmWindowCornerPreferenceDoNotRound,
            _ => throw new ArgumentOutOfRangeException(nameof(preference), preference, null),
        };

        return SetDwmAttribute(hwnd, DwmwaWindowCornerPreference, ref value, "DWM corner preference");
    }

    public PlatformOperationResult ApplyBorderlessPopup(IntPtr hwnd)
    {
        var handleValidation = ValidateHandle(hwnd);
        if (!handleValidation.IsSuccess)
        {
            return handleValidation;
        }

        var styleResult = GetWindowLongPtrChecked(hwnd, GwlStyle, "GetWindowLongPtr(GWL_STYLE) failed.");
        if (!styleResult.IsSuccess)
        {
            return PlatformOperationResult.Failure(styleResult.Error!);
        }

        var exStyleResult = GetWindowLongPtrChecked(hwnd, GwlExStyle, "GetWindowLongPtr(GWL_EXSTYLE) failed.");
        if (!exStyleResult.IsSuccess)
        {
            return PlatformOperationResult.Failure(exStyleResult.Error!);
        }

        var newStyle = ToIntPtr(((long)styleResult.Value | WsPopup) & ~(WsCaption | WsThickFrame | WsMinimizeBox | WsMaximizeBox | WsSysMenu));
        var newExStyle = ToIntPtr(((long)exStyleResult.Value | WsExToolWindow) & ~WsExAppWindow);

        var setStyle = SetWindowLongPtrChecked(hwnd, GwlStyle, newStyle, "SetWindowLongPtr(GWL_STYLE) failed.");
        if (!setStyle.IsSuccess)
        {
            return setStyle;
        }

        var setExStyle = SetWindowLongPtrChecked(hwnd, GwlExStyle, newExStyle, "SetWindowLongPtr(GWL_EXSTYLE) failed.");
        if (!setExStyle.IsSuccess)
        {
            return setExStyle;
        }

        if (!SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged))
        {
            return WindowLastError("SetWindowPos failed.");
        }

        return PlatformOperationResult.Success();
    }

    private static PlatformOperationResult ValidateHandle(IntPtr hwnd) =>
        hwnd == IntPtr.Zero
            ? PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.Failed,
                "Window handle must not be zero."))
            : PlatformOperationResult.Success();

    private static PlatformOperationResult SetDwmAttribute(
        IntPtr hwnd,
        int attribute,
        ref int value,
        string operation)
    {
        var hresult = DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));
        if (hresult == 0)
        {
            return PlatformOperationResult.Success();
        }

        return PlatformOperationResult.Failure(ToDwmError(hresult, operation));
    }

    private static PlatformOperationError ToDwmError(int hresult, string operation)
    {
        var code = hresult is EInvalidArg or DwmECompositionDisabled
            ? PlatformOperationErrorCode.UnsupportedVersion
            : PlatformOperationErrorCode.Failed;

        return new PlatformOperationError(
            code,
            $"{operation} failed.",
            $"HRESULT 0x{hresult:X8}",
            hresult);
    }

    private static PlatformOperationResult WindowLastError(string message)
    {
        var lastError = Marshal.GetLastPInvokeError();
        return PlatformOperationResult.Failure(new PlatformOperationError(
            PlatformOperationErrorCode.Failed,
            message,
            lastError == 0 ? null : new Win32Exception(lastError).Message,
            lastError == 0 ? null : lastError));
    }

    private static IntPtr ToIntPtr(long value) =>
        IntPtr.Size == 8 ? new IntPtr(value) : new IntPtr(unchecked((int)value));

    private static PlatformOperationResult<IntPtr> GetWindowLongPtrChecked(
        IntPtr hwnd,
        int index,
        string message)
    {
        Marshal.SetLastPInvokeError(0);
        var value = GetWindowLongPtr(hwnd, index);
        var lastError = Marshal.GetLastPInvokeError();
        if (value == IntPtr.Zero && lastError != 0)
        {
            return PlatformOperation.Failure<IntPtr>(new PlatformOperationError(
                PlatformOperationErrorCode.Failed,
                message,
                new Win32Exception(lastError).Message,
                lastError));
        }

        return PlatformOperation.Success(value);
    }

    private static PlatformOperationResult SetWindowLongPtrChecked(
        IntPtr hwnd,
        int index,
        IntPtr value,
        string message)
    {
        Marshal.SetLastPInvokeError(0);
        var previousValue = SetWindowLongPtr(hwnd, index, value);
        var lastError = Marshal.GetLastPInvokeError();
        return previousValue == IntPtr.Zero && lastError != 0
            ? PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.Failed,
                message,
                new Win32Exception(lastError).Message,
                lastError))
            : PlatformOperationResult.Success();
    }

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int value,
        int size);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr hwndInsertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
