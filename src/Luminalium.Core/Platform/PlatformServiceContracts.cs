namespace Luminalium.Core.Platform;

public enum AutostartState
{
    Disabled,
    Enabled,
}

public enum WindowCornerPreference
{
    Round,
    DoNotRound,
}

public sealed record IdentityResult(
    string Id,
    PlatformOperationWarning? Warning = null);

public interface IAutostartService
{
    PlatformOperationResult Enable();

    PlatformOperationResult Disable();

    PlatformOperationResult<AutostartState> GetState();
}

public interface IUrlProtocolService
{
    PlatformOperationResult Register();

    PlatformOperationResult Unregister();

    PlatformOperationResult<bool> IsRegistered();
}

public interface IMonitorService
{
    PlatformOperationResult<IReadOnlyList<MonitorInfo>> GetMonitors();
}

public interface IMachineIdentityService
{
    PlatformOperationResult<IdentityResult> GetIdentity();
}

public interface INotificationService
{
    PlatformOperationResult<PlatformCapability> IsSupported();

    PlatformOperationResult Show(string title, string body);
}

public interface IWindowStyleService
{
    PlatformOperationResult ApplyBorderColorNone(IntPtr hwnd);

    PlatformOperationResult ApplyDarkTitleBar(IntPtr hwnd, bool enabled);

    PlatformOperationResult ApplyCornerPreference(IntPtr hwnd, WindowCornerPreference preference);

    PlatformOperationResult ApplyBorderlessPopup(IntPtr hwnd);
}
