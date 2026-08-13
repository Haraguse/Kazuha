namespace Luminalium.Core;

/// <summary>
/// Scaffold-level solution identity (Task 3).
/// Product identity and version metadata contracts are owned by Task 6;
/// this type only pins the solution name and the validated Windows floor.
/// </summary>
public static class SolutionInfo
{
    /// <summary>Solution/product name used across project naming.</summary>
    public const string SolutionName = "Luminalium";

    /// <summary>
    /// Windows compatibility floor validated by Task 2
    /// (Windows 10 version 1809, build 17763).
    /// </summary>
    public const string MinimumWindowsVersion = "10.0.17763.0";
}
