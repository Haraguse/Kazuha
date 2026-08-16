using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage.FileIO;

internal abstract class BclStorageItem : IStorageBookmarkItem, IStorageItem, IDisposable, IStorageItemWithFileSystemInfo
{
	public FileSystemInfo FileSystemInfo { get; }

	public string Name => FileSystemInfo.Name;

	public bool CanBookmark => true;

	public Uri Path => GetPathCore(FileSystemInfo);

	protected BclStorageItem(FileSystemInfo fileSystemInfo)
	{
		if ((fileSystemInfo ?? throw new ArgumentNullException("fileSystemInfo")) is DirectoryInfo { Exists: false })
		{
			throw new ArgumentException("Directory must exist", "fileSystemInfo");
		}
		FileSystemInfo = fileSystemInfo;
		base._002Ector();
	}

	public Task<StorageItemProperties> GetBasicPropertiesAsync()
	{
		return Task.FromResult(GetBasicPropertiesAsyncCore(FileSystemInfo));
	}

	public Task<IStorageFolder?> GetParentAsync()
	{
		return Task.FromResult((IStorageFolder)WrapFileSystemInfo(GetParentCore(FileSystemInfo)));
	}

	public Task DeleteAsync()
	{
		DeleteCore(FileSystemInfo);
		return Task.CompletedTask;
	}

	public Task<IStorageItem?> MoveAsync(IStorageFolder destination)
	{
		return Task.FromResult(WrapFileSystemInfo(MoveCore(FileSystemInfo, destination)));
	}

	public Task<string?> SaveBookmarkAsync()
	{
		return Task.FromResult(StorageBookmarkHelper.EncodeBclBookmark(FileSystemInfo.FullName));
	}

	public Task ReleaseBookmarkAsync()
	{
		return Task.CompletedTask;
	}

	public void Dispose()
	{
	}

	[return: NotNullIfNotNull("fileSystemInfo")]
	protected IStorageItem? WrapFileSystemInfo(FileSystemInfo? fileSystemInfo)
	{
		if (!(fileSystemInfo is DirectoryInfo directoryInfo))
		{
			if (fileSystemInfo is FileInfo fileInfo)
			{
				return new BclStorageFile(fileInfo);
			}
			return null;
		}
		return new BclStorageFolder(directoryInfo);
	}

	internal static void DeleteCore(FileSystemInfo fileSystemInfo)
	{
		if (fileSystemInfo is DirectoryInfo directoryInfo)
		{
			directoryInfo.Delete(recursive: true);
		}
		else
		{
			fileSystemInfo.Delete();
		}
	}

	internal static Uri GetPathCore(FileSystemInfo fileSystemInfo)
	{
		try
		{
			if (fileSystemInfo is DirectoryInfo directoryInfo)
			{
				if (directoryInfo.Parent != null)
				{
					goto IL_0026;
				}
			}
			else if (fileSystemInfo is FileInfo { Directory: not null })
			{
				goto IL_0026;
			}
			bool flag = false;
			goto IL_002c;
			IL_0026:
			flag = true;
			goto IL_002c;
			IL_002c:
			if (flag)
			{
				return StorageProviderHelpers.UriFromFilePath(fileSystemInfo.FullName, fileSystemInfo is DirectoryInfo);
			}
		}
		catch (SecurityException)
		{
		}
		return new Uri(fileSystemInfo.Name, UriKind.Relative);
	}

	internal static StorageItemProperties GetBasicPropertiesAsyncCore(FileSystemInfo fileSystemInfo)
	{
		if (fileSystemInfo.Exists)
		{
			return new StorageItemProperties((ulong)((fileSystemInfo is FileInfo fileInfo) ? fileInfo.Length : 0), fileSystemInfo.CreationTimeUtc, fileSystemInfo.LastWriteTimeUtc);
		}
		return new StorageItemProperties();
	}

	internal static DirectoryInfo? GetParentCore(FileSystemInfo fileSystemInfo)
	{
		if (fileSystemInfo is FileInfo fileInfo)
		{
			DirectoryInfo directory = fileInfo.Directory;
			if (directory != null)
			{
				return directory;
			}
		}
		else if (fileSystemInfo is DirectoryInfo directoryInfo)
		{
			DirectoryInfo parent = directoryInfo.Parent;
			if (parent != null)
			{
				return parent;
			}
		}
		return null;
	}

	internal static FileSystemInfo? MoveCore(FileSystemInfo fileSystemInfo, IStorageFolder destination)
	{
		string text = destination?.TryGetLocalPath();
		if (text != null)
		{
			string text2 = System.IO.Path.Combine(text, fileSystemInfo.Name);
			if (fileSystemInfo is DirectoryInfo directoryInfo)
			{
				directoryInfo.MoveTo(text2);
				return new DirectoryInfo(text2);
			}
			if (fileSystemInfo is FileInfo fileInfo)
			{
				fileInfo.MoveTo(text2);
				return new FileInfo(text2);
			}
		}
		return null;
	}

	internal static FileStream OpenReadCore(FileInfo fileInfo)
	{
		return fileInfo.OpenRead();
	}

	internal static FileStream OpenWriteCore(FileInfo fileInfo)
	{
		return new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Write);
	}

	internal static IEnumerable<FileSystemInfo> GetItemsCore(DirectoryInfo directoryInfo)
	{
		return directoryInfo.EnumerateDirectories().OfType<FileSystemInfo>().Concat(directoryInfo.EnumerateFiles());
	}

	internal static FileSystemInfo? GetFolderCore(DirectoryInfo directoryInfo, string name)
	{
		string path = System.IO.Path.Combine(directoryInfo.FullName, name);
		if (Directory.Exists(path))
		{
			return new DirectoryInfo(path);
		}
		return null;
	}

	internal static FileSystemInfo? GetFileCore(DirectoryInfo directoryInfo, string name)
	{
		string text = System.IO.Path.Combine(directoryInfo.FullName, name);
		if (File.Exists(text))
		{
			return new FileInfo(text);
		}
		return null;
	}

	internal static FileInfo CreateFileCore(DirectoryInfo directoryInfo, string name)
	{
		FileInfo fileInfo = new FileInfo(System.IO.Path.Combine(directoryInfo.FullName, name));
		using (fileInfo.Create())
		{
			return fileInfo;
		}
	}

	internal static DirectoryInfo CreateFolderCore(DirectoryInfo directoryInfo, string name)
	{
		return directoryInfo.CreateSubdirectory(name);
	}
}
