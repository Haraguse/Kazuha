using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia;

internal static class ClassBindingManager
{
	private const string ClassPropertyPrefix = "__AvaloniaReserved::Classes::";

	private static readonly Dictionary<string, AvaloniaProperty> s_RegisteredProperties = new Dictionary<string, AvaloniaProperty>();

	public static IDisposable Bind(StyledElement target, string className, BindingBase source, object anchor)
	{
		AvaloniaProperty classProperty = GetClassProperty(className);
		return target.Bind(classProperty, source);
	}

	private static AvaloniaProperty RegisterClassProxyProperty(string className)
	{
		StyledProperty<bool> styledProperty = AvaloniaProperty.Register<StyledElement, bool>("__AvaloniaReserved::Classes::" + className, defaultValue: false);
		styledProperty.Changed.Subscribe(delegate(AvaloniaPropertyChangedEventArgs<bool> args)
		{
			((StyledElement)args.Sender).Classes.Set(className, args.NewValue.GetValueOrDefault());
		});
		return styledProperty;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AvaloniaProperty GetClassProperty(string className)
	{
		string key = "__AvaloniaReserved::Classes::" + className;
		if (!s_RegisteredProperties.TryGetValue(key, out AvaloniaProperty value))
		{
			return s_RegisteredProperties[key] = RegisterClassProxyProperty(className);
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsClassesBindingProperty(AvaloniaProperty property, [NotNullWhen(true)] out string? classPropertyName)
	{
		classPropertyName = null;
		string name = property.Name;
		if (name != null && name.StartsWith("__AvaloniaReserved::Classes::", StringComparison.OrdinalIgnoreCase))
		{
			classPropertyName = property.Name.Substring("__AvaloniaReserved::Classes::".Length + 1);
			return true;
		}
		return false;
	}
}
