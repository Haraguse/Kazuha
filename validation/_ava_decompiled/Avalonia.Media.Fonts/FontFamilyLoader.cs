using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts;

internal static class FontFamilyLoader
{
	/// <summary>
	/// Loads all font assets that belong to the specified <see cref="T:Avalonia.Media.Fonts.FontFamilyKey" />
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static IEnumerable<Uri> LoadFontAssets(Uri source)
	{
		if (source.IsAvares() || source.IsAbsoluteResm())
		{
			if (!IsFontSource(source))
			{
				return GetFontAssetsBySource(source);
			}
			return GetFontAssetsByExpression(source);
		}
		return Enumerable.Empty<Uri>();
	}

	/// <summary>
	/// Determines whether the specified URI refers to a font file source.
	/// </summary>
	/// <param name="uri">The URI to evaluate as a potential font file source. Must not be null.</param>
	/// <returns>true if the URI points to a recognized font file source; otherwise, false.</returns>
	public static bool IsFontSource(Uri uri)
	{
		return IsFontFile(GetSubString(uri.OriginalString, '?'));
	}

	/// <summary>
	/// Determines whether the specified file path refers to a supported font file type.
	/// </summary>
	/// <remarks>This method performs a case-insensitive check for common font file extensions. It
	/// does not verify the existence or validity of the file at the specified path.</remarks>
	/// <param name="filePath">The path of the file to check. Can be a relative or absolute path. If null, the method returns false.</param>
	/// <returns>true if the file path ends with ".ttf", ".otf", or ".ttc" (case-insensitive); otherwise, false.</returns>
	public static bool IsFontFile(string filePath)
	{
		if (filePath == null)
		{
			return false;
		}
		if (!filePath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) && !filePath.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
		{
			return filePath.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	/// <summary>
	/// Searches for font assets at a given location and returns a quantity of found assets
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	private static IEnumerable<Uri> GetFontAssetsBySource(Uri source)
	{
		return from x in AvaloniaLocator.Current.GetRequiredService<IAssetLoader>().GetAssets(source, null)
			where IsFontSource(x)
			select x;
	}

	/// <summary>
	/// Searches for font assets at a given location and only accepts assets that fit to a given filename expression.
	/// <para>File names can target multiple files with * wildcard. For example "FontFile*.ttf"</para>
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	private static IEnumerable<Uri> GetFontAssetsByExpression(Uri source)
	{
		(string, string) fileName = GetFileName(source, out Uri location);
		string item = fileName.Item1;
		string extension = fileName.Item2;
		string filePattern = CreateFilePattern(source, location, item);
		return from x in AvaloniaLocator.Current.GetRequiredService<IAssetLoader>().GetAssets(location, null)
			where IsContainsFile(x, filePattern, extension)
			select x;
	}

	private static (string fileNameWithoutExtension, string extension) GetFileName(Uri source, out Uri location)
	{
		if (source.IsAbsoluteResm())
		{
			(string, string) fileNameAndExtension = GetFileNameAndExtension(source.GetUnescapeAbsolutePath(), '.');
			string uriString = source.GetUnescapeAbsoluteUri().Replace("." + fileNameAndExtension.Item1 + fileNameAndExtension.Item2, string.Empty);
			location = new Uri(uriString, UriKind.RelativeOrAbsolute);
			return fileNameAndExtension;
		}
		(string, string) fileNameAndExtension2 = GetFileNameAndExtension(source.OriginalString);
		string oldValue = fileNameAndExtension2.Item1 + fileNameAndExtension2.Item2;
		string uriString2 = source.GetUnescapeAbsoluteUri().Replace(oldValue, string.Empty);
		location = new Uri(uriString2);
		return fileNameAndExtension2;
	}

	private static string CreateFilePattern(Uri source, Uri location, string fileNameWithoutExtension)
	{
		string unescapeAbsolutePath = location.GetUnescapeAbsolutePath();
		string subString = GetSubString(fileNameWithoutExtension, '*');
		if (!source.IsAbsoluteResm())
		{
			return unescapeAbsolutePath + subString;
		}
		return unescapeAbsolutePath + "." + subString;
	}

	private static bool IsContainsFile(Uri x, string filePattern, string fileExtension)
	{
		string unescapeAbsolutePath = x.GetUnescapeAbsolutePath();
		if (unescapeAbsolutePath.IndexOf(filePattern, StringComparison.Ordinal) >= 0)
		{
			return unescapeAbsolutePath.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static (string fileNameWithoutExtension, string extension) GetFileNameAndExtension(string path, char directorySeparator = '/')
	{
		ReadOnlySpan<char> readOnlySpan = path.AsSpan();
		readOnlySpan = (IsPathRooted(readOnlySpan, directorySeparator) ? readOnlySpan.Slice(1, path.Length - 1) : readOnlySpan);
		ReadOnlySpan<char> fileExtension = GetFileExtension(readOnlySpan);
		if (fileExtension.Length == readOnlySpan.Length)
		{
			return (fileNameWithoutExtension: fileExtension.ToString(), extension: string.Empty);
		}
		return (fileNameWithoutExtension: GetFileName(readOnlySpan, directorySeparator, fileExtension.Length).ToString(), extension: fileExtension.ToString());
	}

	private static bool IsPathRooted(ReadOnlySpan<char> path, char directorySeparator)
	{
		if (path.Length > 0)
		{
			return path[0] == directorySeparator;
		}
		return false;
	}

	private static ReadOnlySpan<char> GetFileExtension(ReadOnlySpan<char> path)
	{
		for (int num = path.Length - 1; num > 0; num--)
		{
			if (path[num] == '.')
			{
				return path.Slice(num, path.Length - num);
			}
		}
		return path;
	}

	private static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path, char directorySeparator, int extensionLength)
	{
		for (int num = path.Length - extensionLength - 1; num >= 0; num--)
		{
			if (path[num] == directorySeparator)
			{
				return path.Slice(num + 1, path.Length - num - extensionLength - 1);
			}
		}
		return path.Slice(0, path.Length - extensionLength);
	}

	private static string GetSubString(string path, char separator)
	{
		for (int i = 0; i < path.Length; i++)
		{
			if (path[i] == separator)
			{
				return path.Substring(0, i);
			}
		}
		return path;
	}
}
