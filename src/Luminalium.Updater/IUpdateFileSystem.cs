namespace Luminalium.Updater;

public interface IUpdateFileSystem
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    void CreateDirectory(string path);

    IEnumerable<string> EnumerateFiles(string directory);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    void DeleteFile(string path);

}

public sealed class PhysicalUpdateFileSystem : IUpdateFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public IEnumerable<string> EnumerateFiles(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            : [];

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(sourcePath, destinationPath, overwrite);
    }

    public void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
