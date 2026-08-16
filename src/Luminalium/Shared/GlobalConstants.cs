using Avalonia.Media;

namespace Luminalium.Shared;

public static class GlobalConstants
{
#if DEBUG
    public static bool IsDevelopment => true;
#else
    public static bool IsDevelopment => false;
#endif
    
    public static string PlatformExecutableExtension => OperatingSystem.IsWindows() ? ".exe" : "";
    
    public static FontFamily FluentIconsFontFamily { get; } =
        new("avares://Luminalium/Assets/Fonts/#FluentSystemIcons-Resizable");
}