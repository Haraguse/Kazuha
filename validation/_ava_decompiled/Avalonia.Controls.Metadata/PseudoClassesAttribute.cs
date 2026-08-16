using System;
using System.Collections.Generic;

namespace Avalonia.Controls.Metadata;

/// <summary>
/// Defines all pseudoclasses by name referenced and implemented by a control.
/// </summary>
/// <remarks>
/// This is currently used for code-completion in certain IDEs.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class PseudoClassesAttribute : Attribute
{
	/// <summary>
	/// Gets the list of pseudoclass names.
	/// </summary>
	public IReadOnlyList<string> PseudoClasses { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Controls.Metadata.PseudoClassesAttribute" /> class.
	/// </summary>
	/// <param name="pseudoClasses">The list of pseudoclass names.</param>
	public PseudoClassesAttribute(params string[] pseudoClasses)
	{
		PseudoClasses = pseudoClasses;
	}
}
