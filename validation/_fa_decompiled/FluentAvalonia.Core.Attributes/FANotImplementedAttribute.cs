using System;

namespace FluentAvalonia.Core.Attributes;

/// <summary>
/// Marks that a property or class has not been implemented in the FluentAvalonia version
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class FANotImplementedAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="T:System.NotImplementedException" />  class.
	/// </summary>
	public FANotImplementedAttribute()
	{
	}
}
