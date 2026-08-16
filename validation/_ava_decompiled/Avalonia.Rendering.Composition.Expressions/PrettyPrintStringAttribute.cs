using System;

namespace Avalonia.Rendering.Composition.Expressions;

[AttributeUsage(AttributeTargets.Field)]
internal sealed class PrettyPrintStringAttribute : Attribute
{
	public string Name { get; }

	public PrettyPrintStringAttribute(string name)
	{
		Name = name;
	}
}
