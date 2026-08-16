using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Avalonia.Platform;

/// <inheritdoc cref="T:Avalonia.Platform.IAssetLoader" />
public static class AssetLoader
{
	private static IAssetLoader GetAssetLoader()
	{
		return AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
	}

	/// <inheritdoc cref="M:Avalonia.Platform.IAssetLoader.SetDefaultAssembly(System.Reflection.Assembly)" />
	public static void SetDefaultAssembly(Assembly assembly)
	{
		GetAssetLoader().SetDefaultAssembly(assembly);
	}

	/// <inheritdoc cref="M:Avalonia.Platform.IAssetLoader.Exists(System.Uri,System.Uri)" />
	public static bool Exists(Uri uri, Uri? baseUri = null)
	{
		return GetAssetLoader().Exists(uri, baseUri);
	}

	/// <inheritdoc cref="M:Avalonia.Platform.IAssetLoader.Open(System.Uri,System.Uri)" />
	public static Stream Open(Uri uri, Uri? baseUri = null)
	{
		return GetAssetLoader().Open(uri, baseUri);
	}

	/// <inheritdoc cref="M:Avalonia.Platform.IAssetLoader.OpenAndGetAssembly(System.Uri,System.Uri)" />
	public static (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri? baseUri = null)
	{
		return GetAssetLoader().OpenAndGetAssembly(uri, baseUri);
	}

	/// <inheritdoc cref="M:Avalonia.Platform.IAssetLoader.GetAssembly(System.Uri,System.Uri)" />
	public static Assembly? GetAssembly(Uri uri, Uri? baseUri = null)
	{
		return GetAssetLoader().GetAssembly(uri, baseUri);
	}

	/// <inheritdoc cref="M:Avalonia.Platform.IAssetLoader.GetAssets(System.Uri,System.Uri)" />
	public static IEnumerable<Uri> GetAssets(Uri uri, Uri? baseUri)
	{
		return GetAssetLoader().GetAssets(uri, baseUri);
	}

	/// <inheritdoc cref="M:Avalonia.Platform.IAssetLoader.InvalidateAssemblyCache" />
	public static void InvalidateAssemblyCache(string name)
	{
		GetAssetLoader().InvalidateAssemblyCache(name);
	}

	/// <inheritdoc cref="M:Avalonia.Platform.IAssetLoader.InvalidateAssemblyCache(System.String)" />
	public static void InvalidateAssemblyCache()
	{
		GetAssetLoader().InvalidateAssemblyCache();
	}

	internal static void RegisterResUriParsers()
	{
		if (!UriParser.IsKnownScheme("avares"))
		{
			UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority | GenericUriParserOptions.NoUserInfo | GenericUriParserOptions.NoPort | GenericUriParserOptions.NoQuery | GenericUriParserOptions.NoFragment), "avares", -1);
		}
	}
}
