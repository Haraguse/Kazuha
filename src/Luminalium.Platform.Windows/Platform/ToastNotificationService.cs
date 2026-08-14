using System.Runtime.InteropServices;
using Luminalium.Core.Platform;

namespace Luminalium.Platform.Windows.Platform;

public sealed class ToastNotificationService : INotificationService
{
    public const string AppUserModelId = "Kazuha.Luminalium";

    private const string AppUserModelIdKeyPath = @"Software\Classes\AppUserModelId\Kazuha.Luminalium";

    private readonly IRegistryAdapter _registry;

    public ToastNotificationService(IRegistryAdapter registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public PlatformOperationResult<PlatformCapability> IsSupported()
    {
        var keyExists = _registry.KeyExists(RegistryHiveKind.CurrentUser, AppUserModelIdKeyPath);
        if (!keyExists.IsSuccess)
        {
            return PlatformOperation.Failure<PlatformCapability>(keyExists.Error!);
        }

        if (keyExists.Value)
        {
            return PlatformOperation.Success(new PlatformCapability(
                true,
                "WindowsToastNotification"));
        }

        return PlatformOperation.Success(new PlatformCapability(
            false,
            "WindowsToastNotification",
            new PlatformOperationError(
                PlatformOperationErrorCode.Unavailable,
                "AppUserModelID is not registered for this unpackaged app."),
            AppUserModelIdKeyPath));
    }

    public PlatformOperationResult Show(string title, string body)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(body);

        var supported = IsSupported();
        if (!supported.IsSuccess)
        {
            return PlatformOperationResult.Failure(supported.Error!);
        }

        if (supported.Value is { IsSupported: false })
        {
            return PlatformOperationResult.Failure(supported.Value.Reason!);
        }

        return SetProcessAppUserModelId();
    }

    public static PlatformOperationResult SetProcessAppUserModelId()
    {
        var hresult = SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
        if (hresult == 0)
        {
            return PlatformOperationResult.Success();
        }

        return PlatformOperationResult.Failure(new PlatformOperationError(
            PlatformOperationErrorCode.Failed,
            "SetCurrentProcessExplicitAppUserModelID failed.",
            $"HRESULT 0x{hresult:X8}",
            hresult));
    }

    [DllImport("shell32.dll", EntryPoint = "SetCurrentProcessExplicitAppUserModelID", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
