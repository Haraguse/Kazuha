using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime;

/// <summary>
/// Provides the parents for the current XAML node in an eager way, avoiding allocations when possible.
/// </summary>
/// <remarks>This interface is used by the XAML compiler and shouldn't be implemented in your code.</remarks>
public interface IAvaloniaXamlIlEagerParentStackProvider : IAvaloniaXamlIlParentStackProvider
{
	/// <summary>
	/// Gets the directly available parents (which don't include ones returned by parent providers).
	/// The parents are returned in reverse order:
	/// the last element is the most direct parent while the first element is the most distant ancestor.
	/// </summary>
	IReadOnlyList<object> DirectParentsStack { get; }

	/// <summary>
	/// Gets the parent <see cref="T:Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlEagerParentStackProvider" />, if available.
	/// </summary>
	IAvaloniaXamlIlEagerParentStackProvider? ParentProvider { get; }
}
