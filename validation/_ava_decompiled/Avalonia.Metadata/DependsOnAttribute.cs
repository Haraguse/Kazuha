using System;

namespace Avalonia.Metadata;

/// <summary>
/// Indicates that the property depends on the value of another property in markup.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
public sealed class DependsOnAttribute : Attribute
{
	/// <summary>
	/// Gets the name of the property that this property depends on.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Metadata.DependsOnAttribute" /> class.
	/// </summary>
	/// <param name="propertyName">
	/// The name of the property that this property depends on.
	/// </param>
	public DependsOnAttribute(string propertyName)
	{
		Name = propertyName;
	}
}
