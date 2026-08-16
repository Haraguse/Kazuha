using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.Parsers;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.Converters;

[RequiresUnreferencedCode("XamlTypeResolver might require unreferenced code.")]
public class AvaloniaPropertyTypeConverter : TypeConverter
{
	/// <inheritdoc />
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(string);
	}

	/// <inheritdoc />
	public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		AvaloniaPropertyRegistry instance = AvaloniaPropertyRegistry.Instance;
		(string? ns, string? owner, string name) tuple = PropertyParser.Parse(new CharacterReader(((string)value).AsSpan()));
		string item = tuple.ns;
		string item2 = tuple.owner;
		string item3 = tuple.name;
		Type type = TryResolveOwnerByName(context, item, item2);
		Type type2 = context?.GetFirstParent<ControlTemplate>()?.TargetType ?? context?.GetFirstParent<Style>()?.Selector?.TargetType ?? typeof(Control);
		if ((object)type == null)
		{
			type = type2;
		}
		Type type3 = type;
		AvaloniaProperty avaloniaProperty = instance.FindRegistered(type3, item3);
		if (avaloniaProperty == null)
		{
			throw new XamlLoadException($"Could not find property '{type3.Name}.{item3}'.");
		}
		if (type3 != type2 && !avaloniaProperty.IsAttached && !AvaloniaPropertyRegistry.Instance.IsRegistered(type2, avaloniaProperty))
		{
			Logger.TryGet(LogEventLevel.Warning, "Property")?.Log(this, "Property '{Owner}.{Name}' is not registered on '{Type}'.", type3, item3, type2);
		}
		return avaloniaProperty;
	}

	[RequiresUnreferencedCode("XamlTypeResolver might require unreferenced code.")]
	private static Type? TryResolveOwnerByName(ITypeDescriptorContext? context, string? ns, string? owner)
	{
		if (owner != null)
		{
			Type obj = context?.ResolveType(ns, owner);
			if (obj == null)
			{
				string text = (string.IsNullOrEmpty(ns) ? owner : (ns + ":" + owner));
				throw new XamlLoadException("Could not find type '" + text + "'.");
			}
			return obj;
		}
		return null;
	}
}
