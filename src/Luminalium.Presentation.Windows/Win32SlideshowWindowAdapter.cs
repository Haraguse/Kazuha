using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Luminalium.Presentation;

namespace Luminalium.Presentation.Windows;

public sealed class Win32SlideshowWindowAdapter : ISlideshowWindowAdapter
{
    private const int WindowTextBufferLength = 512;
    private const uint MapVirtualKeyToScanCode = 0;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int SwShow = 5;

    private static readonly HashSet<string> PowerPointClasses = new(StringComparer.Ordinal)
    {
        "screenClass",
    };

    private static readonly HashSet<string> WpsClasses = new(StringComparer.Ordinal)
    {
        "wppSlideShowWindowClass",
        "WPP SlideShow Window",
        "WPP SlideShow Window 8.0",
    };

    private static readonly HashSet<string> PowerPointProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "powerpnt.exe",
    };

    private static readonly HashSet<string> WpsProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "wpp.exe",
        "kwpp.exe",
    };

    private static readonly string[] StrictTitleHints =
    [
        "slide show",
        "slideshow",
        "slide-show",
        "幻灯片放映",
        "幻燈片放映",
        "投影片放映",
        "スライド ショー",
        "スライドショー",
        "슬라이드 쇼",
        "슬라이드쇼",
        "diaporama",
        "mode diaporama",
        "bildschirmprasentation",
        "bildschirmpräsentation",
        "presentacion con diapositivas",
        "presentación con diapositivas",
        "apresentacao de slides",
        "apresentação de slides",
    ];

    public Task<PresentationOperationResult<SlideshowWindowInfo?>> FindSlideshowWindowAsync(
        PresentationHostKind? preferredKind = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var foreground = GetForegroundWindow();
            if (foreground != 0 && TryBuildWindowInfo(foreground, preferredKind, out var foregroundInfo))
            {
                return Task.FromResult(PresentationOperation.Success<SlideshowWindowInfo?>(foregroundInfo));
            }

            SlideshowWindowInfo? found = null;
            var callback = new EnumWindowsProc((hwnd, _) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (TryBuildWindowInfo(hwnd, preferredKind, out var info))
                {
                    found = info;
                    return false;
                }

                return true;
            });

            if (!EnumWindows(callback, IntPtr.Zero))
            {
                if (found is not null)
                {
                    return Task.FromResult(PresentationOperation.Success<SlideshowWindowInfo?>(found));
                }

                var error = ToLastWin32Error("EnumWindows failed while searching for a slideshow window.");
                return Task.FromResult(PresentationOperation.Failure<SlideshowWindowInfo?>(error));
            }

            return Task.FromResult(PresentationOperation.Success<SlideshowWindowInfo?>(found));
        }
        catch (Exception exception) when (exception is OperationCanceledException or Win32Exception)
        {
            return Task.FromResult(PresentationOperation.Failure<SlideshowWindowInfo?>(
                PresentationErrors.FromException(exception, "searching for a slideshow window")));
        }
    }

    public Task<PresentationOperationResult<PresentationWindowRect>> GetWindowRectAsync(
        nint hwnd,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (hwnd == 0)
        {
            return Task.FromResult(PresentationOperation.Failure<PresentationWindowRect>(PresentationErrors.SlideshowClosed()));
        }

        if (!GetWindowRect(hwnd, out var rect))
        {
            return Task.FromResult(PresentationOperation.Failure<PresentationWindowRect>(ToLastWin32Error("GetWindowRect failed for slideshow window.")));
        }

        return Task.FromResult(PresentationOperation.Success(new PresentationWindowRect(
            rect.Left,
            rect.Top,
            rect.Right - rect.Left,
            rect.Bottom - rect.Top)));
    }

    public Task<PresentationOperationResult> PostKeyAsync(
        nint hwnd,
        PresentationVirtualKey virtualKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PostKeyPress(hwnd, virtualKey));
    }

    public Task<PresentationOperationResult> PostCtrlShortcutAsync(
        nint hwnd,
        PresentationVirtualKey virtualKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (hwnd == 0)
        {
            return Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.SlideshowClosed()));
        }

        var ctrl = PresentationVirtualKey.Control;
        if (!PostMessage(hwnd, WmKeyDown, (nuint)ctrl, BuildKeyLParam(ctrl, keyUp: false)))
        {
            return Task.FromResult(PresentationOperationResult.Failure(ToLastWin32Error("PostMessage failed for Ctrl keydown.")));
        }

        if (!PostMessage(hwnd, WmKeyDown, (nuint)virtualKey, BuildKeyLParam(virtualKey, keyUp: false))
            || !PostMessage(hwnd, WmKeyUp, (nuint)virtualKey, BuildKeyLParam(virtualKey, keyUp: true))
            || !PostMessage(hwnd, WmKeyUp, (nuint)ctrl, BuildKeyLParam(ctrl, keyUp: true)))
        {
            return Task.FromResult(PresentationOperationResult.Failure(ToLastWin32Error("PostMessage failed for Ctrl shortcut.")));
        }

        return Task.FromResult(PresentationOperationResult.Success());
    }

    public Task<PresentationOperationResult> FocusAsync(nint hwnd, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (hwnd == 0)
        {
            return Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.SlideshowClosed()));
        }

        _ = ShowWindow(hwnd, SwShow);
        _ = BringWindowToTop(hwnd);
        _ = SetForegroundWindow(hwnd);
        return Task.FromResult(PresentationOperationResult.Success());
    }

    internal static bool TitleLooksLikeSlideshow(string title)
    {
        var normalized = (title ?? string.Empty).Trim().ToLowerInvariant();
        return normalized.Length > 0 && StrictTitleHints.Any(hint => normalized.Contains(hint, StringComparison.Ordinal));
    }

    internal static nint BuildKeyLParam(PresentationVirtualKey virtualKey, bool keyUp)
    {
        var scanCode = MapVirtualKey((uint)virtualKey, MapVirtualKeyToScanCode);
        var value = 1 | ((int)scanCode << 16);
        if (keyUp)
        {
            value = unchecked(value | (int)0xC0000000);
        }

        return value;
    }

    private static PresentationOperationResult PostKeyPress(nint hwnd, PresentationVirtualKey virtualKey)
    {
        if (hwnd == 0)
        {
            return PresentationOperationResult.Failure(PresentationErrors.SlideshowClosed());
        }

        if (!PostMessage(hwnd, WmKeyDown, (nuint)virtualKey, BuildKeyLParam(virtualKey, keyUp: false))
            || !PostMessage(hwnd, WmKeyUp, (nuint)virtualKey, BuildKeyLParam(virtualKey, keyUp: true)))
        {
            return PresentationOperationResult.Failure(ToLastWin32Error("PostMessage failed for slideshow key press."));
        }

        return PresentationOperationResult.Success();
    }

    private bool TryBuildWindowInfo(nint hwnd, PresentationHostKind? preferredKind, out SlideshowWindowInfo info)
    {
        info = default!;
        if (hwnd == 0 || !IsWindowVisible(hwnd))
        {
            return false;
        }

        var className = GetClassName(hwnd);
        var processName = GetProcessName(hwnd);
        var title = GetWindowTitle(hwnd);
        var kind = KindFromClass(className) ?? KindFromProcess(processName);
        var classMatches = ClassMatchesKind(className, preferredKind);
        var processMatches = ProcessMatchesKind(processName, preferredKind);
        var isMatch = classMatches || (processMatches && TitleLooksLikeSlideshow(title));
        if (!isMatch)
        {
            return false;
        }

        if (preferredKind is not null and not PresentationHostKind.None && kind != preferredKind)
        {
            return false;
        }

        var rectResult = GetWindowRectAsync(hwnd).GetAwaiter().GetResult();
        info = new SlideshowWindowInfo(
            hwnd,
            kind ?? PresentationHostKind.None,
            className,
            processName,
            title,
            rectResult.IsSuccess ? rectResult.Value : default);
        return true;
    }

    private static bool ClassMatchesKind(string className, PresentationHostKind? kind) =>
        kind switch
        {
            PresentationHostKind.PowerPoint => PowerPointClasses.Contains(className),
            PresentationHostKind.Wps => WpsClasses.Contains(className),
            PresentationHostKind.None or null => PowerPointClasses.Contains(className) || WpsClasses.Contains(className),
            _ => false,
        };

    private static bool ProcessMatchesKind(string processName, PresentationHostKind? kind) =>
        kind switch
        {
            PresentationHostKind.PowerPoint => PowerPointProcessNames.Contains(processName),
            PresentationHostKind.Wps => WpsProcessNames.Contains(processName),
            PresentationHostKind.None or null => PowerPointProcessNames.Contains(processName) || WpsProcessNames.Contains(processName),
            _ => false,
        };

    private static PresentationHostKind? KindFromClass(string className)
    {
        if (PowerPointClasses.Contains(className))
        {
            return PresentationHostKind.PowerPoint;
        }

        if (WpsClasses.Contains(className))
        {
            return PresentationHostKind.Wps;
        }

        return null;
    }

    private static PresentationHostKind? KindFromProcess(string processName)
    {
        if (PowerPointProcessNames.Contains(processName))
        {
            return PresentationHostKind.PowerPoint;
        }

        if (WpsProcessNames.Contains(processName))
        {
            return PresentationHostKind.Wps;
        }

        return null;
    }

    private static string GetClassName(nint hwnd)
    {
        var buffer = new char[256];
        var length = GetClassName(hwnd, buffer, buffer.Length);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    private static string GetWindowTitle(nint hwnd)
    {
        var buffer = new char[WindowTextBufferLength];
        var length = GetWindowText(hwnd, buffer, buffer.Length);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    private static string GetProcessName(nint hwnd)
    {
        _ = GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return string.Empty;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            var processName = (process.ProcessName ?? string.Empty).Trim().ToLowerInvariant();
            return processName.Contains('.', StringComparison.Ordinal) ? processName : processName + ".exe";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or Win32Exception)
        {
            return string.Empty;
        }
    }

    private static PresentationError ToLastWin32Error(string message)
    {
        var lastError = Marshal.GetLastPInvokeError();
        var detail = lastError == 0 ? null : new Win32Exception(lastError).Message;
        return PresentationErrors.CommandRejected(message, detail);
    }

    private delegate bool EnumWindowsProc(nint hwnd, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hwnd);

    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "GetClassNameW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint hwnd, [Out] char[] className, int maxCount);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hwnd, [Out] char[] text, int maxCount);

    [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

    [DllImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hwnd, out RectNative rect);

    [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint hwnd, int message, nuint wParam, nint lParam);

    [DllImport("user32.dll", EntryPoint = "MapVirtualKeyW")]
    private static extern uint MapVirtualKey(uint code, uint mapType);

    [DllImport("user32.dll", EntryPoint = "ShowWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint hwnd, int commandShow);

    [DllImport("user32.dll", EntryPoint = "BringWindowToTop", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(nint hwnd);

    [DllImport("user32.dll", EntryPoint = "SetForegroundWindow", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hwnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct RectNative
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
