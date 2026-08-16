using System;

namespace Avalonia.Metadata;

/// <summary>
/// Attribute that instructs the compiler to resolve the data type using specific scope hints, such as Style or ControlTemplate.
/// </summary>
/// <remarks>
/// This attribute is used to configure markup extensions like TemplateBinding to properly parse AvaloniaProperty values,
/// targeting a specific scope data type.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class InheritDataTypeFromAttribute : Attribute
{
	/// <summary>
	/// Gets the kind of scope from which the data type should be inherited.
	/// </summary>
	public InheritDataTypeFromScopeKind ScopeKind { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Metadata.InheritDataTypeFromAttribute" /> class with the specified scope kind.
	/// </summary>
	/// <param name="scopeKind">The kind of scope from which to inherit the data type.</param>
	public InheritDataTypeFromAttribute(InheritDataTypeFromScopeKind scopeKind)
	{
		ScopeKind = scopeKind;
	}
}
