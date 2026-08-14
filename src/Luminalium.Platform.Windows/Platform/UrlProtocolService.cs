using Luminalium.Core.Platform;

namespace Luminalium.Platform.Windows.Platform;

public sealed class UrlProtocolService : IUrlProtocolService
{
    private readonly IRegistryAdapter _registry;
    private readonly string _schemeKeyPath;
    private readonly string _commandKeyPath;
    private readonly string _iconKeyPath;
    private readonly string _schemeDisplayName;
    private readonly string _command;
    private readonly string _icon;

    public UrlProtocolService(IRegistryAdapter registry, string scheme, string executablePath)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrWhiteSpace(scheme);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        _registry = registry;
        _schemeKeyPath = $@"Software\Classes\{scheme}";
        _commandKeyPath = $@"{_schemeKeyPath}\shell\open\command";
        _iconKeyPath = $@"{_schemeKeyPath}\DefaultIcon";
        _schemeDisplayName = $"URL:{scheme} Protocol";
        _command = $"\"{executablePath}\" \"%1\"";
        _icon = $"\"{executablePath}\",0";
    }

    public PlatformOperationResult Register()
    {
        var existingCommand = _registry.GetStringValue(RegistryHiveKind.CurrentUser, _commandKeyPath, string.Empty);
        if (!existingCommand.IsSuccess)
        {
            return PlatformOperationResult.Failure(existingCommand.Error!);
        }

        if (string.Equals(existingCommand.Value, _command, StringComparison.Ordinal))
        {
            return PlatformOperationResult.AlreadyAppliedSuccess();
        }

        return FirstFailureOrSuccess(
            _registry.SetStringValue(RegistryHiveKind.CurrentUser, _schemeKeyPath, string.Empty, _schemeDisplayName),
            _registry.SetStringValue(RegistryHiveKind.CurrentUser, _schemeKeyPath, "URL Protocol", string.Empty),
            _registry.SetStringValue(RegistryHiveKind.CurrentUser, _commandKeyPath, string.Empty, _command),
            _registry.SetStringValue(RegistryHiveKind.CurrentUser, _iconKeyPath, string.Empty, _icon));
    }

    public PlatformOperationResult Unregister() =>
        FirstFailureOrSuccess(
            _registry.DeleteKey(RegistryHiveKind.CurrentUser, _commandKeyPath),
            _registry.DeleteKey(RegistryHiveKind.CurrentUser, $@"{_schemeKeyPath}\shell\open"),
            _registry.DeleteKey(RegistryHiveKind.CurrentUser, $@"{_schemeKeyPath}\shell"),
            _registry.DeleteKey(RegistryHiveKind.CurrentUser, _iconKeyPath),
            _registry.DeleteKey(RegistryHiveKind.CurrentUser, _schemeKeyPath));

    public PlatformOperationResult<bool> IsRegistered()
    {
        var existingCommand = _registry.GetStringValue(RegistryHiveKind.CurrentUser, _commandKeyPath, string.Empty);
        if (!existingCommand.IsSuccess)
        {
            return PlatformOperation.Failure<bool>(existingCommand.Error!);
        }

        return PlatformOperation.Success(
            string.Equals(existingCommand.Value, _command, StringComparison.Ordinal));
    }

    private static PlatformOperationResult FirstFailureOrSuccess(params PlatformOperationResult[] results)
    {
        foreach (var result in results)
        {
            if (!result.IsSuccess)
            {
                return result;
            }
        }

        return PlatformOperationResult.Success();
    }
}
