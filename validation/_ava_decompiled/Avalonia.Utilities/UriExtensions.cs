using System;

namespace Avalonia.Utilities;

internal static class UriExtensions
{
	public static bool IsAbsoluteResm(this Uri uri)
	{
		if (uri.IsAbsoluteUri)
		{
			return uri.IsResm();
		}
		return false;
	}

	public static bool IsResm(this Uri uri)
	{
		return uri.Scheme == "resm";
	}

	public static bool IsAvares(this Uri uri)
	{
		return uri.Scheme == "avares";
	}

	public static bool IsFontCollection(this Uri uri)
	{
		return uri.Scheme == "fonts";
	}

	public static Uri EnsureAbsolute(this Uri uri, Uri? baseUri)
	{
		if (uri.IsAbsoluteUri)
		{
			return uri;
		}
		if (baseUri == null)
		{
			throw new ArgumentException($"Relative uri {uri} without base url");
		}
		if (!baseUri.IsAbsoluteUri)
		{
			throw new ArgumentException($"Base uri {baseUri} is relative");
		}
		return new Uri(baseUri, uri);
	}

	public static string GetUnescapeAbsolutePath(this Uri uri)
	{
		return Uri.UnescapeDataString(uri.AbsolutePath);
	}

	public static string GetUnescapeAbsoluteUri(this Uri uri)
	{
		return Uri.UnescapeDataString(uri.AbsoluteUri);
	}

	public static string GetAssemblyNameFromQuery(this Uri uri)
	{
		string text = Uri.UnescapeDataString(uri.Query);
		int num;
		for (num = 1; num < text.Length; num++)
		{
			bool flag = false;
			for (int i = 0; i < "assembly".Length && text[num] == "assembly"[i]; i++)
			{
				flag = i == "assembly".Length - 1;
				num++;
			}
			num++;
			int num2 = num;
			for (; num < text.Length && text[num] != '&'; num++)
			{
			}
			if (flag)
			{
				return text.Substring(num2, num - num2);
			}
		}
		return "";
	}
}
