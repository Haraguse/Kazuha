using Luminalium.Core.Platform;

namespace Luminalium.Platform.Windows.Platform;

public sealed class MachineIdentityService : IMachineIdentityService
{
    private const string CryptographyPath = @"SOFTWARE\Microsoft\Cryptography";
    private const string MachineGuidValueName = "MachineGuid";

    private readonly IRegistryAdapter _registry;
    private string? _fallbackId;

    public MachineIdentityService(IRegistryAdapter registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public PlatformOperationResult<IdentityResult> GetIdentity()
    {
        var machineGuid = _registry.GetStringValue(
            RegistryHiveKind.LocalMachine,
            CryptographyPath,
            MachineGuidValueName);

        if (machineGuid.IsSuccess)
        {
            var normalized = machineGuid.Value?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return PlatformOperation.Success(new IdentityResult(normalized));
            }
        }

        _fallbackId ??= Guid.NewGuid().ToString("D").ToLowerInvariant();
        var warning = new PlatformOperationWarning(
            machineGuid.Error?.Code ?? PlatformOperationErrorCode.NotFound,
            "MachineGuid was unavailable; generated a process-lifetime fallback identity.",
            machineGuid.Error?.Message ?? "HKLM MachineGuid value was missing or empty.");

        return PlatformOperation.Success(new IdentityResult(_fallbackId, warning));
    }
}
