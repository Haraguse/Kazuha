using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public class StaticResourceExtension
{
	public object? ResourceKey { get; set; }

	public StaticResourceExtension()
	{
	}

	public StaticResourceExtension(object resourceKey)
	{
		ResourceKey = resourceKey;
	}

	public object? ProvideValue(IServiceProvider serviceProvider)
	{
		return ProvideValue(serviceProvider, ResourceKey);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static object? ProvideValue(IServiceProvider serviceProvider, object? resourceKey)
	{
		if (resourceKey == null)
		{
			throw new ArgumentException("StaticResourceExtension.ResourceKey must be set.");
		}
		IAvaloniaXamlIlParentStackProvider service = serviceProvider.GetService<IAvaloniaXamlIlParentStackProvider>();
		IProvideValueTarget service2 = serviceProvider.GetService<IProvideValueTarget>();
		object obj = service2?.TargetObject;
		object obj2 = service2?.TargetProperty;
		object obj3 = ((obj2 is AvaloniaProperty avaloniaProperty) ? avaloniaProperty : ((!(obj2 is PropertyInfo info)) ? service2?.TargetProperty : new ReflectionClrPropertyInfo(info)));
		object obj4 = obj3;
		ThemeVariant themeVariant = (obj as IThemeVariantHost)?.ActualThemeVariant ?? GetDictionaryVariant(service);
		Type type = ((obj4 is AvaloniaProperty avaloniaProperty2) ? avaloniaProperty2.PropertyType : ((!(obj4 is IPropertyInfo propertyInfo)) ? null : propertyInfo.PropertyType));
		Type type2 = type;
		if (obj is Setter setter)
		{
			AvaloniaProperty property = setter.Property;
			if ((object)property != null)
			{
				type2 = property.PropertyType;
			}
		}
		if (service != null)
		{
			using (Diagnostic.FindingResource()?.AddTag("Key", resourceKey).AddTag("ThemeVariant", themeVariant))
			{
				if (service is IAvaloniaXamlIlEagerParentStackProvider provider)
				{
					EagerParentStackEnumerator eagerParentStackEnumerator = new EagerParentStackEnumerator(provider);
					while (true)
					{
						IResourceNode resourceNode = eagerParentStackEnumerator.TryGetNextOfType<IResourceNode>();
						if (resourceNode != null)
						{
							if (!resourceNode.TryGetResource(resourceKey, themeVariant, out object value))
							{
								continue;
							}
							obj2 = ColorToBrushConverter.Convert(value, type2);
							goto IL_023e;
						}
						break;
					}
				}
				else
				{
					foreach (object parent in service.Parents)
					{
						if (!(parent is IResourceNode resourceNode2) || !resourceNode2.TryGetResource(resourceKey, themeVariant, out object value2))
						{
							continue;
						}
						obj2 = ColorToBrushConverter.Convert(value2, type2);
						goto IL_023e;
					}
				}
			}
		}
		if (obj is Control target && obj4 is IPropertyInfo property2)
		{
			Type localTargetType = type2;
			object localKeyInstance = resourceKey;
			DelayedBinding.Add(target, property2, (StyledElement x) => ColorToBrushConverter.Convert(x.FindResource(localKeyInstance), localTargetType));
			return AvaloniaProperty.UnsetValue;
		}
		throw new KeyNotFoundException($"Static resource '{resourceKey}' not found.");
		IL_023e:
		return obj2;
	}

	internal static ThemeVariant? GetDictionaryVariant(IAvaloniaXamlIlParentStackProvider? stack)
	{
		if (stack != null)
		{
			if (stack is IAvaloniaXamlIlEagerParentStackProvider provider)
			{
				EagerParentStackEnumerator eagerParentStackEnumerator = new EagerParentStackEnumerator(provider);
				while (true)
				{
					IThemeVariantProvider themeVariantProvider = eagerParentStackEnumerator.TryGetNextOfType<IThemeVariantProvider>();
					if (themeVariantProvider == null)
					{
						break;
					}
					ThemeVariant key = themeVariantProvider.Key;
					if ((object)key != null)
					{
						return key;
					}
				}
				return null;
			}
			foreach (object parent in stack.Parents)
			{
				if (parent is IThemeVariantProvider themeVariantProvider2)
				{
					ThemeVariant key2 = themeVariantProvider2.Key;
					if ((object)key2 != null)
					{
						return key2;
					}
				}
			}
			return null;
		}
		return null;
	}
}
