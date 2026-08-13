namespace Luminalium.Platform.Windows;

/// <summary>
/// Minimal Windows platform boundary marker (Task 3).
/// Windows-specific services (windowing, notifications, SMTC, registry)
/// are implemented by later tasks; this type documents the OS floor
/// and provides a host check used by scaffold tests.
/// </summary>
public static class WindowsPlatformInfo
{
    /// <summary>TFM consumed from the Task 2 validated stack.</summary>
    public const string TargetFramework = "net10.0-windows10.0.17763.0";

    /// <summary>
    /// True when the current OS is Windows 10 build 17763 (version 1809)
    /// or newer. Mirrors the README compatibility claim.
    /// </summary>
    public static bool IsSupportedWindowsVersion =>
        OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);
}
