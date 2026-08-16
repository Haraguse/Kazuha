using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Utilities;

namespace Avalonia.Platform.Storage.FileIO;

internal sealed class BclStorageFolder(DirectoryInfo directoryInfo) : BclStorageItem(directoryInfo), IStorageBookmarkFolder, IStorageFolder, IStorageItem, IDisposable, IStorageBookmarkItem
{
	public IAsyncEnumerable<IStorageItem> GetItemsAsync()
	{
		return (from f in BclStorageItem.GetItemsCore(directoryInfo).Select(base.WrapFileSystemInfo)
			where f != null
			select f).AsAsyncEnumerable();
	}

	public Task<IStorageFile?> CreateFileAsync(string name)
	{
		return Task.FromResult((IStorageFile)WrapFileSystemInfo(BclStorageItem.CreateFileCore(directoryInfo, name)));
	}

	public Task<IStorageFolder?> CreateFolderAsync(string name)
	{
		return Task.FromResult((IStorageFolder)WrapFileSystemInfo(BclStorageItem.CreateFolderCore(directoryInfo, name)));
	}

	public Task<IStorageFolder?> GetFolderAsync(string name)
	{
		return Task.FromResult((IStorageFolder)WrapFileSystemInfo(BclStorageItem.GetFolderCore(directoryInfo, name)));
	}

	public Task<IStorageFile?> GetFileAsync(string name)
	{
		return Task.FromResult((IStorageFile)WrapFileSystemInfo(BclStorageItem.GetFileCore(directoryInfo, name)));
	}
}
