using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Logging;

namespace Avalonia.Platform.Storage.FileIO;

internal abstract class BclStorageProvider : IStorageProvider
{
	private static readonly Guid s_folderDownloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");

	public abstract bool CanOpen { get; }

	public abstract bool CanSave { get; }

	public abstract bool CanPickFolder { get; }

	public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
	{
		return (await OpenFilePickerWithResultAsync(options).ConfigureAwait(continueOnCapturedContext: false)).Files;
	}

	public abstract Task<OpenFilePickerResult> OpenFilePickerWithResultAsync(FilePickerOpenOptions options);

	public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
	{
		return (await SaveFilePickerWithResultAsync(options).ConfigureAwait(continueOnCapturedContext: false)).File;
	}

	public abstract Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options);

	public abstract Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options);

	public virtual Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
	{
		return Task.FromResult(OpenBookmark(bookmark) as IStorageBookmarkFile);
	}

	public virtual Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
	{
		return Task.FromResult(OpenBookmark(bookmark) as IStorageBookmarkFolder);
	}

	public virtual Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath)
	{
		if (filePath.IsAbsoluteUri)
		{
			FileInfo fileInfo = new FileInfo(filePath.LocalPath);
			if (fileInfo.Exists)
			{
				return Task.FromResult((IStorageFile)new BclStorageFile(fileInfo));
			}
		}
		return Task.FromResult<IStorageFile>(null);
	}

	public virtual Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
	{
		if (folderPath.IsAbsoluteUri)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(folderPath.LocalPath);
			if (directoryInfo.Exists)
			{
				return Task.FromResult((IStorageFolder)new BclStorageFolder(directoryInfo));
			}
		}
		return Task.FromResult<IStorageFolder>(null);
	}

	public virtual Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
	{
		DirectoryInfo directoryInfo = TryGetWellKnownFolderCore(wellKnownFolder);
		if (directoryInfo != null)
		{
			return Task.FromResult((IStorageFolder)new BclStorageFolder(directoryInfo));
		}
		return Task.FromResult<IStorageFolder>(null);
	}

	internal static DirectoryInfo? TryGetWellKnownFolderCore(WellKnownFolder wellKnownFolder)
	{
		string text = wellKnownFolder switch
		{
			WellKnownFolder.Desktop => GetFromSpecialFolder(Environment.SpecialFolder.Desktop), 
			WellKnownFolder.Documents => GetFromSpecialFolder(Environment.SpecialFolder.Personal), 
			WellKnownFolder.Downloads => GetDownloadsWellKnownFolder(), 
			WellKnownFolder.Music => GetFromSpecialFolder(Environment.SpecialFolder.MyMusic), 
			WellKnownFolder.Pictures => GetFromSpecialFolder(Environment.SpecialFolder.MyPictures), 
			WellKnownFolder.Videos => GetFromSpecialFolder(Environment.SpecialFolder.MyVideos), 
			_ => throw new ArgumentOutOfRangeException("wellKnownFolder", wellKnownFolder, null), 
		};
		if (text == null)
		{
			return null;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(text);
		if (!directoryInfo.Exists)
		{
			return null;
		}
		return directoryInfo;
		static string GetFromSpecialFolder(Environment.SpecialFolder folder)
		{
			return Environment.GetFolderPath(folder, Environment.SpecialFolderOption.Create);
		}
	}

	protected static string? GetDownloadsWellKnownFolder()
	{
		if (OperatingSystem.IsWindows())
		{
			if (Environment.OSVersion.Version.Major >= 6)
			{
				return TryGetWindowsKnownFolder(s_folderDownloads);
			}
			return null;
		}
		if (OperatingSystem.IsLinux())
		{
			string environmentVariable = Environment.GetEnvironmentVariable("XDG_DOWNLOAD_DIR");
			if (environmentVariable != null && Directory.Exists(environmentVariable))
			{
				return environmentVariable;
			}
		}
		if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
		{
			return "~/Downloads";
		}
		return null;
	}

	private IStorageBookmarkItem? OpenBookmark(string bookmark)
	{
		try
		{
			if (StorageBookmarkHelper.TryDecodeBclBookmark(bookmark, out string localPath))
			{
				return StorageProviderHelpers.TryCreateBclStorageItem(localPath);
			}
			return null;
		}
		catch (Exception propertyValue)
		{
			Logger.TryGet(LogEventLevel.Information, "Platform")?.Log(this, "Unable to read file bookmark: {Exception}", propertyValue);
			return null;
		}
	}

	private unsafe static string? TryGetWindowsKnownFolder(Guid guid)
	{
		char* ptr = null;
		string result = null;
		if (SHGetKnownFolderPath(&guid, 0u, null, &ptr) == 0)
		{
			result = Marshal.PtrToStringUni((nint)ptr);
		}
		if (ptr != null)
		{
			Marshal.FreeCoTaskMem((nint)ptr);
		}
		return result;
	}

	[DllImport("shell32.dll", ExactSpelling = true)]
	private unsafe static extern int SHGetKnownFolderPath(Guid* rfid, uint dwFlags, void* hToken, char** ppszPath);
}
