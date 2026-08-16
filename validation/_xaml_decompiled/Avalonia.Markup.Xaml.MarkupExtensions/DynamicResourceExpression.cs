using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

internal class DynamicResourceExpression : UntypedBindingExpressionBase
{
	private readonly object _resourceKey;

	private readonly object? _anchor;

	private IResourceHost? _host;

	private IResourceProvider? _provider;

	private bool _overrideThemeVariant;

	private bool _targetTypeIsBrush;

	private ThemeVariant? _themeVariant;

	public override string Description => $"DynamicResource {_resourceKey}";

	public DynamicResourceExpression(object resourceKey, object? anchor, ThemeVariant? themeVariant, BindingPriority priority)
		: base(priority)
	{
		_resourceKey = resourceKey;
		_anchor = anchor;
		_themeVariant = themeVariant;
	}

	protected override void StartCore()
	{
		if (!TryGetResourceHost(out _host) && _anchor is IResourceProvider resourceProvider)
		{
			_provider = resourceProvider;
			_host = resourceProvider.Owner;
			_overrideThemeVariant = (object)_themeVariant != null;
		}
		if (_host == null && _provider == null)
		{
			Log("Unable to find IResourceHost or IResourceProvider from which to lookup " + $"DynamicResource {_resourceKey}.", LogEventLevel.Error);
			return;
		}
		if (_provider != null)
		{
			_provider.OwnerChanged += OnResourceProviderOwnerChanged;
		}
		Subscribe(_host);
		_targetTypeIsBrush = base.TargetType == typeof(IBrush);
		PublishValue();
	}

	protected override void StopCore()
	{
		if (_provider != null)
		{
			_provider.OwnerChanged -= OnResourceProviderOwnerChanged;
		}
		Unsubscribe(_host);
		_host = null;
		_provider = null;
	}

	private void OnResourceProviderOwnerChanged(object? sender, EventArgs e)
	{
		Unsubscribe(_host);
		_host = _provider?.Owner;
		Subscribe(_host);
		PublishValue();
	}

	private void ResourcesChanged(object? sender, ResourcesChangedEventArgs e)
	{
		PublishValue();
	}

	private void ActualThemeVariantChanged(object? sender, EventArgs e)
	{
		if (base.IsRunning)
		{
			_themeVariant = ((IThemeVariantHost)sender).ActualThemeVariant;
			PublishValue();
		}
	}

	private void PublishValue()
	{
		if (_host != null)
		{
			ThemeVariant themeVariant = _themeVariant;
			object obj = _host.FindResource(themeVariant, _resourceKey) ?? AvaloniaProperty.UnsetValue;
			object value = (_targetTypeIsBrush ? ColorToBrushConverter.Convert(obj, typeof(IBrush)) : obj);
			PublishValue(value);
		}
		else
		{
			PublishValue(AvaloniaProperty.UnsetValue);
		}
	}

	private bool TryGetResourceHost([NotNullWhen(true)] out IResourceHost? host)
	{
		if (TryGetTarget(out AvaloniaObject target) && target is IResourceHost resourceHost)
		{
			host = resourceHost;
			return true;
		}
		if (_anchor is IResourceHost resourceHost2)
		{
			host = resourceHost2;
			return host != null;
		}
		host = null;
		return false;
	}

	private void Subscribe(IResourceHost? host)
	{
		if (host != null)
		{
			host.ResourcesChanged += ResourcesChanged;
			if (!_overrideThemeVariant && _host is IThemeVariantHost themeVariantHost)
			{
				_themeVariant = themeVariantHost.ActualThemeVariant;
				themeVariantHost.ActualThemeVariantChanged += ActualThemeVariantChanged;
			}
		}
	}

	private void Unsubscribe(IResourceHost? host)
	{
		if (host != null)
		{
			host.ResourcesChanged -= ResourcesChanged;
			if (!_overrideThemeVariant && _host is IThemeVariantHost themeVariantHost)
			{
				themeVariantHost.ActualThemeVariantChanged -= ActualThemeVariantChanged;
			}
		}
	}
}
