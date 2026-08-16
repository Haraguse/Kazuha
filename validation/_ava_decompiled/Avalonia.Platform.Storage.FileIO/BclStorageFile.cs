using System;
using System.IO;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage.FileIO;

internal sealed class BclStorageFile(FileInfo fileInfo) : BclStorageItem(fileInfo), IStorageBookmarkFile, IStorageFile, IStorageItem, IDisposable, IStorageBookmarkItem
{
	public Task<Stream> OpenReadAsync()
	{
		return Task.FromResult((Stream)BclStorageItem.OpenReadCore(fileInfo));
	}

	public Task<Stream> OpenWriteAsync()
	{
		return Task.FromResult((Stream)BclStorageItem.OpenWriteCore(fileInfo));
	}
}
