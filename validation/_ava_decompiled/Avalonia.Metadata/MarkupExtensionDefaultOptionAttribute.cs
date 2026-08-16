using System;

namespace Avalonia.Metadata;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MarkupExtensionDefaultOptionAttribute : Attribute
{
}
