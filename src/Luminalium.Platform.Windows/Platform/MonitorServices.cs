using System.ComponentModel;
using System.Runtime.InteropServices;
using Luminalium.Core.Platform;

namespace Luminalium.Platform.Windows.Platform;

public interface IMonitorAdapter
{
    PlatformOperationResult<IReadOnlyList<MonitorInfo>> GetMonitors();
}

public sealed class MonitorService : IMonitorService
{
    private readonly IMonitorAdapter _adapter;

    public MonitorService(IMonitorAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        _adapter = adapter;
    }

    public PlatformOperationResult<IReadOnlyList<MonitorInfo>> GetMonitors() =>
        _adapter.GetMonitors();
}

public sealed class Win32MonitorAdapter : IMonitorAdapter
{
    private const int MonitorInfoPrimaryFlag = 1;
    private const int MdTEffectiveDpi = 0;

    public PlatformOperationResult<IReadOnlyList<MonitorInfo>> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();
        PlatformOperationError? callbackError = null;
        var callback = new MonitorEnumProc((monitor, _, _, _) =>
        {
            var info = new MonitorInfoNative
            {
                cbSize = (uint)Marshal.SizeOf<MonitorInfoNative>(),
            };

            if (!GetMonitorInfo(monitor, ref info))
            {
                callbackError = ToLastWin32Error("GetMonitorInfoW failed.");
                return false;
            }

            var dpiResult = GetDpiForMonitor(monitor, MdTEffectiveDpi, out var dpiX, out _);
            if (dpiResult != 0)
            {
                callbackError = new PlatformOperationError(
                    PlatformOperationErrorCode.Failed,
                    "GetDpiForMonitor failed.",
                    $"HRESULT 0x{dpiResult:X8}",
                    dpiResult);
                return false;
            }

            monitors.Add(new MonitorInfo(
                info.szDevice,
                new DisplayRect(info.rcMonitor.left, info.rcMonitor.top, info.rcMonitor.right, info.rcMonitor.bottom),
                dpiX / 96d,
                (info.dwFlags & MonitorInfoPrimaryFlag) == MonitorInfoPrimaryFlag));

            return true;
        });

        if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero))
        {
            return PlatformOperation.Failure<IReadOnlyList<MonitorInfo>>(
                callbackError ?? ToLastWin32Error("EnumDisplayMonitors failed."));
        }

        return PlatformOperation.Success<IReadOnlyList<MonitorInfo>>(monitors);
    }

    private static PlatformOperationError ToLastWin32Error(string message)
    {
        var lastError = Marshal.GetLastPInvokeError();
        var detail = lastError == 0 ? null : new Win32Exception(lastError).Message;
        return new PlatformOperationError(PlatformOperationErrorCode.Failed, message, detail, lastError == 0 ? null : lastError);
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [DllImport("user32.dll", EntryPoint = "EnumDisplayMonitors", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr clipRect,
        MonitorEnumProc callback,
        IntPtr data);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoNative monitorInfo);

    [DllImport("shcore.dll", EntryPoint = "GetDpiForMonitor")]
    private static extern int GetDpiForMonitor(
        IntPtr monitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY);

    [StructLayout(LayoutKind.Sequential)]
    private struct RectNative
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoNative
    {
        public uint cbSize;
        public RectNative rcMonitor;
        public RectNative rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }
}
