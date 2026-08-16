using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Platform.Storage;

public static class LauncherExtensions
{
	/// <summary>
	/// Starts the default app associated with the specified storage file.
	/// </summary>
	/// <param name="launcher">ILauncher instance.</param>
	/// <param name="fileInfo">The file.</param>
	public static Task<bool> LaunchFileInfoAsync(this ILauncher launcher, FileInfo fileInfo)
	{
		if (fileInfo == null)
		{
			throw new ArgumentNullException("fileInfo");
		}
		if (!fileInfo.Exists)
		{
			return Task.FromResult(result: false);
		}
		return launcher.LaunchFileAsync(new BclStorageFile(fileInfo));
	}

	/// <summary>
	/// Starts the default app associated with the specified storage directory (folder).
	/// </summary>
	/// <param name="launcher">ILauncher instance.</param>
	/// <param name="directoryInfo">The directory.</param>
	public static Task<bool> LaunchDirectoryInfoAsync(this ILauncher launcher, DirectoryInfo directoryInfo)
	{
		if (directoryInfo == null)
		{
			throw new ArgumentNullException("directoryInfo");
		}
		if (!directoryInfo.Exists)
		{
			return Task.FromResult(result: false);
		}
		return launcher.LaunchFileAsync(new BclStorageFolder(directoryInfo));
	}
}
