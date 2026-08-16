namespace Luminalium.Shared;

public static class Utils
{
    #region Config
    
    private static string GetPath(params string[] strings)
    {
        var appBase = AppContext.BaseDirectory;
        return Path.Combine([appBase, "data", ..strings]);
    }

    public static string GetFilePath(params string[] strings)
    {
        var path = GetPath(strings);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        return path;
    }

    public static string GetDirectoryPath(params string[] strings)
    {
        var path = GetPath(strings);

        if (!string.IsNullOrEmpty(path) && !Directory.Exists(path)) Directory.CreateDirectory(path);

        return path;
    }

    #endregion
}