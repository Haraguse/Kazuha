namespace Luminalium.Core.Platform;

public enum RegistryHiveKind
{
    CurrentUser,
    LocalMachine,
}

public interface IRegistryAdapter
{
    PlatformOperationResult<string?> GetStringValue(
        RegistryHiveKind hive,
        string path,
        string name);

    PlatformOperationResult SetStringValue(
        RegistryHiveKind hive,
        string path,
        string name,
        string value);

    PlatformOperationResult DeleteValue(
        RegistryHiveKind hive,
        string path,
        string name);

    PlatformOperationResult DeleteKey(
        RegistryHiveKind hive,
        string path);

    PlatformOperationResult<bool> KeyExists(RegistryHiveKind hive, string path);
}
