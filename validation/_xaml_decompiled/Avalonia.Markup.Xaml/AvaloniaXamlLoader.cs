using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml;

/// <summary>
/// Loads XAML for a avalonia application.
/// </summary>
public static class AvaloniaXamlLoader
{
	internal interface IRuntimeXamlLoader
	{
		object Load(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration);
	}

	/// <summary>
	/// Loads the XAML into a Avalonia component.
	/// </summary>
	/// <param name="obj">The object to load the XAML into.</param>
	public static void Load(object obj)
	{
		throw new XamlLoadException($"No precompiled XAML found for {obj.GetType()}, make sure to specify x:Class and include your XAML file as AvaloniaResource");
	}

	/// <summary>
	/// Loads the XAML into a Avalonia component.
	/// </summary>
	/// <param name="sp">The parent's service provider.</param>
	/// <param name="obj">The object to load the XAML into.</param>
	public static void Load(IServiceProvider? sp, object obj)
	{
		throw new XamlLoadException($"No precompiled XAML found for {obj.GetType()}, make sure to specify x:Class and include your XAML file as AvaloniaResource");
	}

	/// <summary>
	/// Loads XAML from a URI.
	/// </summary>
	/// <param name="uri">The URI of the XAML file.</param>
	/// <param name="baseUri">
	/// A base URI to use if <paramref name="uri" /> is relative.
	/// </param>
	/// <returns>The loaded object.</returns>
	[RequiresUnreferencedCode("AvaloniaXamlLoader.Load(uri, baseUri) dynamically loads referenced assembly with Avalonia resources.")]
	public static object Load(Uri uri, Uri? baseUri = null)
	{
		return Load(null, uri, baseUri);
	}

	/// <summary>
	/// Loads XAML from a URI.
	/// </summary>
	/// <param name="sp">The parent's service provider.</param>
	/// <param name="uri">The URI of the XAML file.</param>
	/// <param name="baseUri">
	/// A base URI to use if <paramref name="uri" /> is relative.
	/// </param>
	/// <returns>The loaded object.</returns>
	[RequiresUnreferencedCode("AvaloniaXamlLoader.Load(uri, baseUri) dynamically loads referenced assembly with Avalonia resources.")]
	public static object Load(IServiceProvider? sp, Uri uri, Uri? baseUri = null)
	{
		if ((object)uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		IAssetLoader service = AvaloniaLocator.Current.GetService<IAssetLoader>();
		if (service == null)
		{
			throw new InvalidOperationException("Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
		}
		Uri uri2 = (uri.IsAbsoluteUri ? uri : new Uri(baseUri ?? throw new InvalidOperationException("Cannot load relative Uri when BaseUri is null"), uri));
		MethodInfo methodInfo = service.GetAssembly(uri, baseUri)?.GetType("CompiledAvaloniaXaml.!XamlLoader")?.GetMethod("TryLoad", new Type[2]
		{
			typeof(IServiceProvider),
			typeof(string)
		});
		if (methodInfo != null)
		{
			object obj = methodInfo.Invoke(null, new object[2]
			{
				sp,
				uri2.ToString()
			});
			if (obj != null)
			{
				return obj;
			}
		}
		else
		{
			methodInfo = service.GetAssembly(uri, baseUri)?.GetType("CompiledAvaloniaXaml.!XamlLoader")?.GetMethod("TryLoad", new Type[1] { typeof(string) });
			if (methodInfo != null)
			{
				object obj2 = methodInfo.Invoke(null, new object[1] { uri2.ToString() });
				if (obj2 != null)
				{
					return obj2;
				}
			}
		}
		IRuntimeXamlLoader service2 = AvaloniaLocator.Current.GetService<IRuntimeXamlLoader>();
		if (service2 != null)
		{
			(Stream, Assembly) tuple = service.OpenAndGetAssembly(uri, baseUri);
			using Stream stream = tuple.Item1;
			RuntimeXamlLoaderDocument document = new RuntimeXamlLoaderDocument(uri2, stream)
			{
				ServiceProvider = sp
			};
			RuntimeXamlLoaderConfiguration configuration = new RuntimeXamlLoaderConfiguration
			{
				LocalAssembly = tuple.Item2
			};
			return service2.Load(document, configuration);
		}
		throw new XamlLoadException($"No precompiled XAML found for {uri} (baseUri: {baseUri}), make sure to specify x:Class and include your XAML file as AvaloniaResource");
	}
}
