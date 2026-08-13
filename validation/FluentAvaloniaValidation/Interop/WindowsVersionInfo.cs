using System.Runtime.InteropServices;

namespace FluentAvaloniaValidation.Interop;

/// <summary>A Windows OS version, used to report the runtime floor truthfully.</summary>
public readonly record struct WindowsOsVersion(int Major, int Minor, int Build)
{
    public bool IsAtLeast(int major, int minor, int build) =>
        Major > major
        || (Major == major && (Minor > minor || (Minor == minor && Build >= build)));

    public override string ToString() => $"{Major}.{Minor}.{Build}";
}

/// <summary>Reads the real Windows version via RtlGetVersion (not the appcompat-shimmed value).</summary>
public static class WindowsVersionInfo
{
    public static WindowsOsVersion Get()
    {
        var osvi = new RtlOSVersionInfoEx { dwOSVersionInfoSize = Marshal.SizeOf<RtlOSVersionInfoEx>() };
        if (RtlGetVersion(ref osvi) == 0)
        {
            return new WindowsOsVersion((int)osvi.dwMajorVersion, (int)osvi.dwMinorVersion, (int)osvi.dwBuildNumber);
        }

        var fallback = Environment.OSVersion.Version;
        return new WindowsOsVersion(fallback.Major, fallback.Minor, fallback.Build);
    }

    [DllImport("ntdll.dll")]
    private static extern int RtlGetVersion(ref RtlOSVersionInfoEx versionInfo);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RtlOSVersionInfoEx
    {
        public int dwOSVersionInfoSize;
        public int dwMajorVersion;
        public int dwMinorVersion;
        public int dwBuildNumber;
        public int dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szCSDVersion;
    }
}
