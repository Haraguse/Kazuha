using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Styling;

/// <summary>
/// Loads a resource dictionary from a specified URL.
/// </summary>
/// <remarks>
/// If used from the XAML code, it is replaced with direct resource dictionary reference.
/// When used in runtime, this type might be unsafe with trimming and AOT.
/// </remarks>
[RequiresUnreferencedCode("StyleInclude and ResourceInclude use AvaloniaXamlLoader.Load which dynamically loads referenced assembly with Avalonia resources. Note, StyleInclude and ResourceInclude defined in XAML are resolved compile time and are safe with trimming and AOT.")]
public class ResourceInclude : IResourceProvider, IResourceNode, IThemeVariantProvider
{
	private readonly IServiceProvider? _serviceProvider;

	private readonly Uri? _baseUri;

	private IResourceDictionary? _loaded;

	private bool _isLoading;

	/// <summary>
	/// Gets the loaded resource dictionary.
	/// </summary>
	public IResourceDictionary Loaded
	{
		get
		{
			if (_loaded == null)
			{
				_isLoading = true;
				Uri uri = Source ?? throw new InvalidOperationException("ResourceInclude.Source must be set.");
				_loaded = (IResourceDictionary)AvaloniaXamlLoader.Load(_serviceProvider, uri, _baseUri);
				_isLoading = false;
			}
			return _loaded;
		}
	}

	public IResourceHost? Owner => Loaded.Owner;

	/// <summary>
	/// Gets or sets the source URL.
	/// </summary>
	public Uri? Source { get; set; }

	ThemeVariant? IThemeVariantProvider.Key { get; set; }

	bool IResourceNode.HasResources => Loaded.HasResources;

	public event EventHandler? OwnerChanged
	{
		add
		{
			Loaded.OwnerChanged += value;
		}
		remove
		{
			Loaded.OwnerChanged -= value;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Markup.Xaml.Styling.ResourceInclude" /> class.
	/// </summary>
	/// <param name="baseUri">The base URL for the XAML context.</param>
	public ResourceInclude(Uri? baseUri)
	{
		_baseUri = baseUri;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Markup.Xaml.Styling.ResourceInclude" /> class.
	/// </summary>
	/// <param name="serviceProvider">The XAML service provider.</param>
	public ResourceInclude(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		_baseUri = serviceProvider.GetContextBaseUri();
	}

	public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
	{
		if (!_isLoading)
		{
			return Loaded.TryGetResource(key, theme, out value);
		}
		value = null;
		return false;
	}

	void IResourceProvider.AddOwner(IResourceHost owner)
	{
		Loaded.AddOwner(owner);
	}

	void IResourceProvider.RemoveOwner(IResourceHost owner)
	{
		Loaded.RemoveOwner(owner);
	}
}
