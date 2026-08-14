using System.Runtime.InteropServices;
using Luminalium.Core.Platform;
using Luminalium.Platform.Windows.Platform;
using Xunit;

namespace Luminalium.Tests;

public sealed class PlatformServicesTests
{
    private const string ExePath = @"C:\Program Files\Luminalium\Luminalium.exe";

    [Fact]
    public void UrlProtocolRegisterIsIdempotentAndUnregisterToleratesMissingKeys()
    {
        var registry = new FakeRegistryAdapter();
        var service = new UrlProtocolService(registry, "luminalium", ExePath);

        var missingUnregister = service.Unregister();
        var registered = service.Register();
        var secondRegister = service.Register();
        var state = service.IsRegistered();

        Assert.True(missingUnregister.IsSuccess);
        Assert.True(registered.IsSuccess);
        Assert.True(secondRegister.IsSuccess);
        Assert.True(secondRegister.AlreadyApplied);
        Assert.True(state.Value);
        Assert.Equal(
            $"\"{ExePath}\" \"%1\"",
            registry.GetRequiredValue(RegistryHiveKind.CurrentUser, @"Software\Classes\luminalium\shell\open\command", string.Empty));
        Assert.Equal(
            string.Empty,
            registry.GetRequiredValue(RegistryHiveKind.CurrentUser, @"Software\Classes\luminalium", "URL Protocol"));
        Assert.Equal(
            $"\"{ExePath}\",0",
            registry.GetRequiredValue(RegistryHiveKind.CurrentUser, @"Software\Classes\luminalium\DefaultIcon", string.Empty));

        var unregister = service.Unregister();
        var unregisteredState = service.IsRegistered();

        Assert.True(unregister.IsSuccess);
        Assert.True(unregisteredState.IsSuccess);
        Assert.False(unregisteredState.Value);
    }

    [Fact]
    public void AutostartEnableAndDisableAreIdempotent()
    {
        var registry = new FakeRegistryAdapter();
        var service = new AutostartService(registry, ExePath);

        var enabled = service.Enable();
        var secondEnable = service.Enable();
        var enabledState = service.GetState();

        Assert.True(enabled.IsSuccess);
        Assert.True(secondEnable.IsSuccess);
        Assert.True(secondEnable.AlreadyApplied);
        Assert.Equal(AutostartState.Enabled, enabledState.Value);
        Assert.Equal(
            $"\"{ExePath}\" --autostart",
            registry.GetRequiredValue(
                RegistryHiveKind.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Run",
                "Luminalium"));

        var disabled = service.Disable();
        var secondDisable = service.Disable();
        var disabledState = service.GetState();

        Assert.True(disabled.IsSuccess);
        Assert.True(secondDisable.IsSuccess);
        Assert.Equal(AutostartState.Disabled, disabledState.Value);
    }

    [Fact]
    public void MonitorServiceReturnsStableMonitorsAndDisplayMathRoundTrips()
    {
        var monitors = new[]
        {
            new MonitorInfo(@"\\.\DISPLAY1", new DisplayRect(0, 0, 3840, 2160), 1.5d, true),
            new MonitorInfo(@"\\.\DISPLAY2", new DisplayRect(3840, 0, 5760, 1080), 1.0d, false),
        };
        var service = new MonitorService(new FakeMonitorAdapter(monitors));

        var result = service.GetMonitors();
        var logicalRect = DisplayMath.PhysicalToLogical(monitors[0].Bounds, monitors[0].ScaleFactor);
        var roundTrippedRect = DisplayMath.LogicalToPhysical(logicalRect, monitors[0].ScaleFactor);
        var point = new DisplayPoint(120, 240);
        var roundTrippedPoint = DisplayMath.LogicalToPhysical(
            DisplayMath.PhysicalToLogical(point, monitors[0].ScaleFactor),
            monitors[0].ScaleFactor);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.True(result.Value[0].IsPrimary);
        Assert.Equal(new DisplayRect(0, 0, 3840, 2160), result.Value[0].Bounds);
        Assert.Equal(1.5d, result.Value[0].ScaleFactor);
        Assert.Equal(new DisplayRect(0, 0, 2560, 1440), logicalRect);
        Assert.Equal(monitors[0].Bounds, roundTrippedRect);
        Assert.Equal(point, roundTrippedPoint);
    }

    [Fact]
    public void MachineIdentityReadsNormalizedMachineGuidAndCachesFallbackWarning()
    {
        var registry = new FakeRegistryAdapter();
        registry.SetStringValue(
            RegistryHiveKind.LocalMachine,
            @"SOFTWARE\Microsoft\Cryptography",
            "MachineGuid",
            "  ABCD-EF01  ");
        var configuredService = new MachineIdentityService(registry);

        var configured = configuredService.GetIdentity();

        Assert.True(configured.IsSuccess);
        Assert.Equal("abcd-ef01", configured.Value!.Id);
        Assert.Null(configured.Value.Warning);

        var fallbackService = new MachineIdentityService(new FakeRegistryAdapter());
        var firstFallback = fallbackService.GetIdentity();
        var secondFallback = fallbackService.GetIdentity();

        Assert.True(Guid.TryParse(firstFallback.Value!.Id, out _));
        Assert.NotNull(firstFallback.Value.Warning);
        Assert.Equal(PlatformOperationErrorCode.NotFound, firstFallback.Value.Warning!.Code);
        Assert.Equal(firstFallback.Value.Id, secondFallback.Value!.Id);
    }

    [Fact]
    public void ToastNotificationCapabilityReportsAumidRegistrationState()
    {
        var unsupportedService = new ToastNotificationService(new FakeRegistryAdapter());

        var unsupported = unsupportedService.IsSupported();
        var unsupportedShow = unsupportedService.Show("Title", "Body");

        Assert.True(unsupported.IsSuccess);
        Assert.False(unsupported.Value!.IsSupported);
        Assert.Equal(PlatformOperationErrorCode.Unavailable, unsupported.Value.Reason!.Code);
        Assert.False(unsupportedShow.IsSuccess);
        Assert.Equal(PlatformOperationErrorCode.Unavailable, unsupportedShow.Error!.Code);

        var registry = new FakeRegistryAdapter();
        registry.SetStringValue(
            RegistryHiveKind.CurrentUser,
            @"Software\Classes\AppUserModelId\Kazuha.Luminalium",
            string.Empty,
            ToastNotificationService.AppUserModelId);
        var supported = new ToastNotificationService(registry).IsSupported();

        Assert.True(supported.IsSuccess);
        Assert.True(supported.Value!.IsSupported);
        Assert.Null(supported.Value.Reason);
    }

    [Fact]
    public void WindowStylesReturnTypedFailuresAndDarkTitleBarTypedResult()
    {
        var service = new WindowStyleService();

        var zeroHandleResult = service.ApplyBorderColorNone(IntPtr.Zero);

        Assert.False(zeroHandleResult.IsSuccess);
        Assert.Equal(PlatformOperationErrorCode.Failed, zeroHandleResult.Error!.Code);

        using var window = NativeTestWindow.TryCreate();
        if (window.Handle == IntPtr.Zero)
        {
            return;
        }

        var darkTitleBar = service.ApplyDarkTitleBar(window.Handle, enabled: true);
        var borderlessPopup = service.ApplyBorderlessPopup(window.Handle);

        Assert.True(darkTitleBar.IsSuccess || darkTitleBar.Error!.Code == PlatformOperationErrorCode.UnsupportedVersion);
        Assert.True(borderlessPopup.IsSuccess);
    }

    private sealed class FakeMonitorAdapter : IMonitorAdapter
    {
        private readonly IReadOnlyList<MonitorInfo> _monitors;

        public FakeMonitorAdapter(IReadOnlyList<MonitorInfo> monitors)
        {
            _monitors = monitors;
        }

        public PlatformOperationResult<IReadOnlyList<MonitorInfo>> GetMonitors() =>
            PlatformOperation.Success(_monitors);
    }

    private sealed class FakeRegistryAdapter : IRegistryAdapter
    {
        private readonly Dictionary<RegistryHiveKind, Dictionary<string, Dictionary<string, string>>> _hives = new()
        {
            [RegistryHiveKind.CurrentUser] = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase),
            [RegistryHiveKind.LocalMachine] = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase),
        };

        public PlatformOperationResult<string?> GetStringValue(
            RegistryHiveKind hive,
            string path,
            string name) =>
            PlatformOperation.Success(
                _hives[hive].TryGetValue(path, out var values) && values.TryGetValue(name, out var value)
                    ? value
                    : null);

        public PlatformOperationResult SetStringValue(
            RegistryHiveKind hive,
            string path,
            string name,
            string value)
        {
            if (!_hives[hive].TryGetValue(path, out var values))
            {
                values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _hives[hive][path] = values;
            }

            values[name] = value;
            return PlatformOperationResult.Success();
        }

        public PlatformOperationResult DeleteValue(RegistryHiveKind hive, string path, string name)
        {
            if (_hives[hive].TryGetValue(path, out var values))
            {
                values.Remove(name);
            }

            return PlatformOperationResult.Success();
        }

        public PlatformOperationResult DeleteKey(RegistryHiveKind hive, string path)
        {
            var keysToRemove = _hives[hive].Keys
                .Where(key => string.Equals(key, path, StringComparison.OrdinalIgnoreCase)
                    || key.StartsWith(path + @"\", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var key in keysToRemove)
            {
                _hives[hive].Remove(key);
            }

            return PlatformOperationResult.Success();
        }

        public PlatformOperationResult<bool> KeyExists(RegistryHiveKind hive, string path) =>
            PlatformOperation.Success(_hives[hive].ContainsKey(path));

        public string GetRequiredValue(RegistryHiveKind hive, string path, string name)
        {
            var result = GetStringValue(hive, path, name);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            return result.Value;
        }
    }

    private sealed class NativeTestWindow : IDisposable
    {
        private const int CwUseDefault = unchecked((int)0x80000000);
        private const int WsOverlappedWindow = 0x00CF0000;

        private static readonly WndProc WindowProcedure = DefWindowProcedure;
        private readonly string _className;

        private NativeTestWindow(string className, IntPtr handle)
        {
            _className = className;
            Handle = handle;
        }

        public IntPtr Handle { get; private set; }

        public static NativeTestWindow TryCreate()
        {
            var className = "LuminaliumPlatformServicesTests" + Guid.NewGuid().ToString("N");
            var instance = GetModuleHandle(null);
            var windowClass = new WndClassEx
            {
                cbSize = (uint)Marshal.SizeOf<WndClassEx>(),
                lpfnWndProc = WindowProcedure,
                hInstance = instance,
                lpszClassName = className,
            };

            var atom = RegisterClassEx(ref windowClass);
            if (atom == 0)
            {
                return new NativeTestWindow(className, IntPtr.Zero);
            }

            var handle = CreateWindowEx(
                0,
                className,
                "Luminalium Test Window",
                WsOverlappedWindow,
                CwUseDefault,
                CwUseDefault,
                100,
                100,
                IntPtr.Zero,
                IntPtr.Zero,
                instance,
                IntPtr.Zero);

            return new NativeTestWindow(className, handle);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                DestroyWindow(Handle);
                Handle = IntPtr.Zero;
            }

            UnregisterClass(_className, GetModuleHandle(null));
        }

        private static IntPtr DefWindowProcedure(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam) =>
            DefWindowProc(hwnd, message, wParam, lParam);

        private delegate IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WndClassEx
        {
            public uint cbSize;
            public uint style;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public WndProc lpfnWndProc;

            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string? lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? moduleName);

        [DllImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
        private static extern ushort RegisterClassEx(ref WndClassEx windowClass);

        [DllImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(
            int exStyle,
            string className,
            string windowName,
            int style,
            int x,
            int y,
            int width,
            int height,
            IntPtr parent,
            IntPtr menu,
            IntPtr instance,
            IntPtr parameter);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "UnregisterClassW", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterClass(string className, IntPtr instance);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    }
}
