using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Metadata;
using Avalonia.Platform.Internal;
using Avalonia.Utilities;

namespace Avalonia.Platform;

/// <summary>
/// Loads assets compiled into the application binary.
/// </summary>
[Unstable("StandardAssetLoader is considered unstable. Please use AssetLoader static class instead.")]
public class StandardAssetLoader : IAssetLoader
{
	private readonly IAssemblyDescriptorResolver _assemblyDescriptorResolver;

	private AssemblyDescriptor? _defaultResmAssembly;

	internal StandardAssetLoader(IAssemblyDescriptorResolver resolver, Assembly? assembly = null)
	{
		if (assembly == null)
		{
			assembly = Assembly.GetEntryAssembly();
		}
		if (assembly != null)
		{
			_defaultResmAssembly = new AssemblyDescriptor(assembly);
		}
		_assemblyDescriptorResolver = resolver;
	}

	public StandardAssetLoader(Assembly? assembly = null)
		: this(new AssemblyDescriptorResolver(), assembly)
	{
	}

	/// <summary>
	/// Sets the default assembly from which to load assets for which no assembly is specified.
	/// </summary>
	/// <param name="assembly">The default assembly.</param>
	public void SetDefaultAssembly(Assembly assembly)
	{
		_defaultResmAssembly = new AssemblyDescriptor(assembly);
	}

	/// <summary>
	/// Checks if an asset with the specified URI exists.
	/// </summary>
	/// <param name="uri">The URI.</param>
	/// <param name="baseUri">
	/// A base URI to use if <paramref name="uri" /> is relative.
	/// </param>
	/// <returns>True if the asset could be found; otherwise false.</returns>
	public bool Exists(Uri uri, Uri? baseUri = null)
	{
		IAssetDescriptor assetDescriptor;
		return TryGetAsset(uri, baseUri, out assetDescriptor);
	}

	/// <summary>
	/// Opens the asset with the requested URI.
	/// </summary>
	/// <param name="uri">The URI.</param>
	/// <param name="baseUri">
	/// A base URI to use if <paramref name="uri" /> is relative.
	/// </param>
	/// <returns>A stream containing the asset contents.</returns>
	/// <exception cref="T:System.IO.FileNotFoundException">
	/// The asset could not be found.
	/// </exception>
	public Stream Open(Uri uri, Uri? baseUri = null)
	{
		return OpenAndGetAssembly(uri, baseUri).stream;
	}

	/// <summary>
	/// Opens the asset with the requested URI and returns the asset stream and the
	/// assembly containing the asset.
	/// </summary>
	/// <param name="uri">The URI.</param>
	/// <param name="baseUri">
	/// A base URI to use if <paramref name="uri" /> is relative.
	/// </param>
	/// <returns>
	/// The stream containing the resource contents together with the assembly.
	/// </returns>
	/// <exception cref="T:System.IO.FileNotFoundException">
	/// The asset could not be found.
	/// </exception>
	public (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri? baseUri = null)
	{
		if (TryGetAsset(uri, baseUri, out IAssetDescriptor assetDescriptor))
		{
			return (stream: assetDescriptor.GetStream(), assembly: assetDescriptor.Assembly);
		}
		throw new FileNotFoundException($"The resource {uri} could not be found.");
	}

	public Assembly? GetAssembly(Uri uri, Uri? baseUri)
	{
		if (!uri.IsAbsoluteUri && baseUri != null)
		{
			uri = new Uri(baseUri, uri);
		}
		if (TryGetAssembly(uri, out IAssemblyDescriptor assembly))
		{
			return assembly.Assembly;
		}
		return null;
	}

	/// <summary>
	/// Gets all assets of a folder and subfolders that match specified uri.
	/// </summary>
	/// <param name="uri">The URI.</param>
	/// <param name="baseUri">Base URI that is used if <paramref name="uri" /> is relative.</param>
	/// <returns>All matching assets as a tuple of the absolute path to the asset and the assembly containing the asset</returns>
	public IEnumerable<Uri> GetAssets(Uri uri, Uri? baseUri)
	{
		if (uri.IsAbsoluteResm())
		{
			if (!TryGetAssembly(uri, out IAssemblyDescriptor assembly))
			{
				assembly = _defaultResmAssembly;
			}
			return assembly?.Resources?.Where((KeyValuePair<string, IAssetDescriptor> x) => x.Key.Contains(uri.GetUnescapeAbsolutePath())).Select((KeyValuePair<string, IAssetDescriptor> x) => new Uri("resm:" + x.Key + "?assembly=" + assembly.Name)) ?? Enumerable.Empty<Uri>();
		}
		uri = uri.EnsureAbsolute(baseUri);
		if (uri.IsAvares())
		{
			if (!TryGetResAsmAndPath(uri, out IAssemblyDescriptor assembly2, out string path))
			{
				return Enumerable.Empty<Uri>();
			}
			if (assembly2?.AvaloniaResources == null)
			{
				return Enumerable.Empty<Uri>();
			}
			if (path.Length > 0 && path[path.Length - 1] != '/')
			{
				path += "/";
			}
			return from x in assembly2.AvaloniaResources
				where x.Key.StartsWith(path, StringComparison.Ordinal)
				select new Uri("avares://" + assembly2.Name + x.Key);
		}
		return Enumerable.Empty<Uri>();
	}

	public void InvalidateAssemblyCache(string name)
	{
		_assemblyDescriptorResolver.InvalidateAssemblyCache(name);
	}

	public void InvalidateAssemblyCache()
	{
		_assemblyDescriptorResolver.InvalidateAssemblyCache();
	}

	public static void RegisterResUriParsers()
	{
		AssetLoader.RegisterResUriParsers();
	}

	private bool TryGetAsset(Uri uri, Uri? baseUri, [NotNullWhen(true)] out IAssetDescriptor? assetDescriptor)
	{
		assetDescriptor = null;
		if (uri.IsAbsoluteResm())
		{
			if (!TryGetAssembly(uri, out IAssemblyDescriptor assembly) && !TryGetAssembly(baseUri, out assembly))
			{
				assembly = _defaultResmAssembly;
			}
			if (assembly?.Resources != null)
			{
				string absolutePath = uri.AbsolutePath;
				if (assembly.Resources.TryGetValue(absolutePath, out assetDescriptor))
				{
					return true;
				}
			}
		}
		uri = uri.EnsureAbsolute(baseUri);
		if (uri.IsAvares() && TryGetResAsmAndPath(uri, out IAssemblyDescriptor assembly2, out string path))
		{
			if (assembly2.AvaloniaResources == null)
			{
				return false;
			}
			if (assembly2.AvaloniaResources.TryGetValue(path, out assetDescriptor))
			{
				return true;
			}
		}
		return false;
	}

	private bool TryGetResAsmAndPath(Uri uri, [NotNullWhen(true)] out IAssemblyDescriptor? assembly, out string path)
	{
		path = uri.GetUnescapeAbsolutePath();
		if (TryLoadAssembly(uri.Authority, out assembly))
		{
			return true;
		}
		return false;
	}

	private bool TryGetAssembly(Uri? uri, [NotNullWhen(true)] out IAssemblyDescriptor? assembly)
	{
		assembly = null;
		if (uri != null)
		{
			if (!uri.IsAbsoluteUri)
			{
				return false;
			}
			if (uri.IsAvares() && TryGetResAsmAndPath(uri, out assembly, out string _))
			{
				return true;
			}
			if (uri.IsResm())
			{
				string assemblyNameFromQuery = uri.GetAssemblyNameFromQuery();
				if (assemblyNameFromQuery.Length > 0 && TryLoadAssembly(assemblyNameFromQuery, out assembly))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool TryLoadAssembly(string assemblyName, [NotNullWhen(true)] out IAssemblyDescriptor? assembly)
	{
		assembly = null;
		try
		{
			assembly = _assemblyDescriptorResolver.GetAssembly(assemblyName);
			return true;
		}
		catch (Exception)
		{
		}
		return false;
	}
}
