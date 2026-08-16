using System;

namespace Avalonia.SourceGenerator;

[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
internal sealed class GenerateCrossThreadProxyAttribute : Attribute
{
	public Type PriorityType { get; }

	public string DefaultPriorityExpression { get; }

	/// <summary>
	/// Optional. Name of the generated proxy class. Defaults to the
	/// interface name with the leading 'I' stripped (if present) and
	/// "Proxy" appended (e.g. <c>IFoo</c> → <c>FooProxy</c>).
	/// </summary>
	public string? GeneratedClassName { get; set; }

	public GenerateCrossThreadProxyAttribute(Type priorityType, string defaultPriorityExpression)
	{
		PriorityType = priorityType;
		DefaultPriorityExpression = defaultPriorityExpression;
	}
}
