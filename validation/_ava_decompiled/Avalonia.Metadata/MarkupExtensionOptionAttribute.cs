using System;

namespace Avalonia.Metadata;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MarkupExtensionOptionAttribute : Attribute
{
	public object Value { get; }

	public int Priority { get; set; }

	public MarkupExtensionOptionAttribute(object value)
	{
		Value = value;
	}
}
