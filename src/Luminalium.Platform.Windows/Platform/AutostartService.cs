using Luminalium.Core.Platform;

namespace Luminalium.Platform.Windows.Platform;

public sealed class AutostartService : IAutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly IRegistryAdapter _registry;
    private readonly string _command;
    private readonly string _valueName;

    public AutostartService(
        IRegistryAdapter registry,
        string executablePath,
        string valueName = "Luminalium")
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueName);

        _registry = registry;
        _command = $"\"{executablePath}\" --autostart";
        _valueName = valueName;
    }

    public PlatformOperationResult Enable()
    {
        var current = _registry.GetStringValue(RegistryHiveKind.CurrentUser, RunKeyPath, _valueName);
        if (!current.IsSuccess)
        {
            return PlatformOperationResult.Failure(current.Error!);
        }

        if (string.Equals(current.Value, _command, StringComparison.Ordinal))
        {
            return PlatformOperationResult.AlreadyAppliedSuccess();
        }

        return _registry.SetStringValue(RegistryHiveKind.CurrentUser, RunKeyPath, _valueName, _command);
    }

    public PlatformOperationResult Disable() =>
        _registry.DeleteValue(RegistryHiveKind.CurrentUser, RunKeyPath, _valueName);

    public PlatformOperationResult<AutostartState> GetState()
    {
        var current = _registry.GetStringValue(RegistryHiveKind.CurrentUser, RunKeyPath, _valueName);
        if (!current.IsSuccess)
        {
            return PlatformOperation.Failure<AutostartState>(current.Error!);
        }

        return PlatformOperation.Success(
            string.Equals(current.Value, _command, StringComparison.Ordinal)
                ? AutostartState.Enabled
                : AutostartState.Disabled);
    }
}
