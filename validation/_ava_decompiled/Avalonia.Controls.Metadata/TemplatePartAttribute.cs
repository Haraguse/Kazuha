using System;

namespace Avalonia.Controls.Metadata;

/// <summary>
/// Defines a control template part referenced by name in code.
/// Template part names should begin with the "PART_" prefix.
/// </summary>
/// <remarks>
/// Style authors should be able to identify the part type used for styling the specific control.
/// The part is usually required in the style and should have a specific predefined name.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class TemplatePartAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the part name used by the class to identify a required element in the style.
	/// Template part names should begin with the "PART_" prefix.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets the type of the element that should be used as a part with name specified
	/// in <see cref="P:Avalonia.Controls.Metadata.TemplatePartAttribute.Name" />.
	/// </summary>
	public Type Type { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the template part is mandatory to be present in the template.
	/// </summary>
	public bool IsRequired { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Controls.Metadata.TemplatePartAttribute" /> class.
	/// </summary>
	public TemplatePartAttribute()
	{
		Name = string.Empty;
		Type = typeof(object);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Controls.Metadata.TemplatePartAttribute" /> class.
	/// </summary>
	/// <param name="name">The part name used by the class to identify a required element in the style.</param>
	/// <param name="type">The type of the element that should be used as a part with name.</param>
	public TemplatePartAttribute(string name, Type type)
	{
		Name = name;
		Type = type;
	}
}
