using System.ComponentModel;
using System.Security;
using Luminalium.Core.Platform;
using Microsoft.Win32;

namespace Luminalium.Platform.Windows.Platform;

public sealed class WindowsRegistryAdapter : IRegistryAdapter
{
    public PlatformOperationResult<string?> GetStringValue(
        RegistryHiveKind hive,
        string path,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(name);

        return Invoke(() =>
        {
            using var key = OpenBaseKey(hive).OpenSubKey(path, writable: false);
            var value = key?.GetValue(name);
            return PlatformOperation.Success(value as string);
        });
    }

    public PlatformOperationResult SetStringValue(
        RegistryHiveKind hive,
        string path,
        string name,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        return Invoke(() =>
        {
            using var key = OpenBaseKey(hive).CreateSubKey(path, writable: true);
            if (key is null)
            {
                return PlatformOperationResult.Failure(new PlatformOperationError(
                    PlatformOperationErrorCode.Failed,
                    $"Registry key '{path}' could not be created."));
            }

            key.SetValue(name, value, RegistryValueKind.String);
            return PlatformOperationResult.Success();
        });
    }

    public PlatformOperationResult DeleteValue(
        RegistryHiveKind hive,
        string path,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(name);

        return Invoke(() =>
        {
            using var key = OpenBaseKey(hive).OpenSubKey(path, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
            return PlatformOperationResult.Success();
        });
    }

    public PlatformOperationResult DeleteKey(RegistryHiveKind hive, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Invoke(() =>
        {
            using var baseKey = OpenBaseKey(hive);
            baseKey.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
            return PlatformOperationResult.Success();
        });
    }

    public PlatformOperationResult<bool> KeyExists(RegistryHiveKind hive, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Invoke(() =>
        {
            using var key = OpenBaseKey(hive).OpenSubKey(path, writable: false);
            return PlatformOperation.Success(key is not null);
        });
    }

    private static RegistryKey OpenBaseKey(RegistryHiveKind hive) =>
        hive switch
        {
            RegistryHiveKind.CurrentUser => RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64),
            RegistryHiveKind.LocalMachine => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64),
            _ => throw new ArgumentOutOfRangeException(nameof(hive), hive, null),
        };

    private static PlatformOperationResult Invoke(Func<PlatformOperationResult> operation)
    {
        try
        {
            return operation();
        }
        catch (UnauthorizedAccessException ex)
        {
            return PlatformOperationResult.Failure(ToError(PlatformOperationErrorCode.AccessDenied, ex));
        }
        catch (SecurityException ex)
        {
            return PlatformOperationResult.Failure(ToError(PlatformOperationErrorCode.AccessDenied, ex));
        }
        catch (IOException ex)
        {
            return PlatformOperationResult.Failure(ToError(PlatformOperationErrorCode.Failed, ex));
        }
        catch (Win32Exception ex)
        {
            return PlatformOperationResult.Failure(ToError(PlatformOperationErrorCode.Failed, ex, ex.NativeErrorCode));
        }
    }

    private static PlatformOperationResult<T> Invoke<T>(Func<PlatformOperationResult<T>> operation)
    {
        try
        {
            return operation();
        }
        catch (UnauthorizedAccessException ex)
        {
            return PlatformOperation.Failure<T>(ToError(PlatformOperationErrorCode.AccessDenied, ex));
        }
        catch (SecurityException ex)
        {
            return PlatformOperation.Failure<T>(ToError(PlatformOperationErrorCode.AccessDenied, ex));
        }
        catch (IOException ex)
        {
            return PlatformOperation.Failure<T>(ToError(PlatformOperationErrorCode.Failed, ex));
        }
        catch (Win32Exception ex)
        {
            return PlatformOperation.Failure<T>(ToError(PlatformOperationErrorCode.Failed, ex, ex.NativeErrorCode));
        }
    }

    private static PlatformOperationError ToError(
        PlatformOperationErrorCode code,
        Exception exception,
        int? nativeErrorCode = null) =>
        new(code, exception.Message, exception.GetType().Name, nativeErrorCode);
}
