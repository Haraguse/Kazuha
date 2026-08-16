using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Avalonia.Platform.Storage.FileIO;

internal static class StorageProviderHelpers
{
	public static BclStorageItem? TryCreateBclStorageItem(string? path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			if (directoryInfo.Exists)
			{
				return new BclStorageFolder(directoryInfo);
			}
			FileInfo fileInfo = new FileInfo(path);
			if (fileInfo.Exists)
			{
				return new BclStorageFile(fileInfo);
			}
		}
		return null;
	}

	public static string? TryGetPathFromFileUri(Uri? uri)
	{
		if ((object)uri == null || !uri.IsAbsoluteUri || !(uri.Scheme == "file"))
		{
			return null;
		}
		return uri.LocalPath;
	}

	public static Uri UriFromFilePath(string path, bool isDirectory)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (path.StartsWith("\\\\?\\", StringComparison.Ordinal))
		{
			stringBuilder.Append(path, 4, path.Length - 4);
		}
		else
		{
			stringBuilder.Append(path);
		}
		stringBuilder = stringBuilder.Replace("%", $"%{37:X2}").Replace("[", $"%{91:X2}").Replace("]", $"%{93:X2}");
		if (!path.EndsWith('/') & isDirectory)
		{
			stringBuilder.Append('/');
		}
		return new UriBuilder("file", string.Empty)
		{
			Path = stringBuilder.ToString()
		}.Uri;
	}

	public static Uri? TryGetUriFromFilePath(string path, bool isDirectory)
	{
		try
		{
			return UriFromFilePath(path, isDirectory);
		}
		catch
		{
			return null;
		}
	}

	[return: NotNullIfNotNull("path")]
	public static string? NameWithExtension(string? path, string? defaultExtension, FilePickerFileType? filter)
	{
		string fileName = Path.GetFileName(path);
		if (fileName != null && !Path.HasExtension(fileName))
		{
			if (filter != null && filter.Patterns?.Count > 0)
			{
				if (defaultExtension != null && filter.Patterns.Contains<string>(defaultExtension))
				{
					return Path.ChangeExtension(path, defaultExtension.TrimStart('.'));
				}
				string text = filter.Patterns.FirstOrDefault((string x) => x != "*.*")?.Split(new string[1] { "*." }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
				if (text != null)
				{
					return Path.ChangeExtension(path, text);
				}
			}
			if (defaultExtension != null)
			{
				return Path.ChangeExtension(path, defaultExtension);
			}
		}
		return path;
	}
}
