using System;
using System.IO;

namespace Avalonia.Platform.Storage;

internal interface IStorageItemWithFileSystemInfo : IStorageItem, IDisposable
{
	FileSystemInfo FileSystemInfo { get; }
}
