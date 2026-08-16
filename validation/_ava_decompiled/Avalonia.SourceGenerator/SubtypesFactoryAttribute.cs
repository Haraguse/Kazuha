using System;

namespace Avalonia.SourceGenerator;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
internal sealed class SubtypesFactoryAttribute : Attribute
{
	public string Namespace { get; }

	public Type BaseType { get; }

	public SubtypesFactoryAttribute(Type baseType, string @namespace)
	{
		BaseType = baseType;
		Namespace = @namespace;
	}
}
