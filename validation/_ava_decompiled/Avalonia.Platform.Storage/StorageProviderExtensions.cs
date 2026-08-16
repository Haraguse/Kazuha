using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Group of public extensions for <see cref="T:Avalonia.Platform.Storage.IStorageProvider" /> class. 
/// </summary>
public static class StorageProviderExtensions
{
	/// <inheritdoc cref="M:Avalonia.Platform.Storage.IStorageProvider.TryGetFileFromPathAsync(System.Uri)" />
	public static Task<IStorageFile?> TryGetFileFromPathAsync(this IStorageProvider provider, string filePath)
	{
		if (provider is BclStorageProvider)
		{
			return Task.FromResult(StorageProviderHelpers.TryCreateBclStorageItem(filePath) as IStorageFile);
		}
		Uri uri = StorageProviderHelpers.TryGetUriFromFilePath(filePath, isDirectory: false);
		if ((object)uri != null)
		{
			return provider.TryGetFileFromPathAsync(uri);
		}
		return Task.FromResult<IStorageFile>(null);
	}

	/// <inheritdoc cref="M:Avalonia.Platform.Storage.IStorageProvider.TryGetFolderFromPathAsync(System.Uri)" />
	public static Task<IStorageFolder?> TryGetFolderFromPathAsync(this IStorageProvider provider, string folderPath)
	{
		if (provider is BclStorageProvider)
		{
			return Task.FromResult(StorageProviderHelpers.TryCreateBclStorageItem(folderPath) as IStorageFolder);
		}
		Uri uri = StorageProviderHelpers.TryGetUriFromFilePath(folderPath, isDirectory: true);
		if ((object)uri != null)
		{
			return provider.TryGetFolderFromPathAsync(uri);
		}
		return Task.FromResult<IStorageFolder>(null);
	}

	/// <summary>
	/// Gets the local file system path of the item as a string.
	/// </summary>
	/// <param name="item">Storage folder or file.</param>
	/// <returns>Full local path to the folder or file if possible, otherwise null.</returns>
	/// <remarks>
	/// Android platform usually uses "content:" virtual file paths
	/// and Browser platform has isolated access without full paths,
	/// so on these platforms this method will return null.
	/// </remarks>
	public static string? TryGetLocalPath(this IStorageItem item)
	{
		if (item is IStorageItemWithFileSystemInfo storageItemWithFileSystemInfo)
		{
			return storageItemWithFileSystemInfo.FileSystemInfo.FullName;
		}
		return StorageProviderHelpers.TryGetPathFromFileUri(item.Path);
	}
}
