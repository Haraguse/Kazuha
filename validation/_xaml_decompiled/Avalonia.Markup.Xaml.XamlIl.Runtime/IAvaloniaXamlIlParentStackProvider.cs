using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.XamlIl.Runtime;

/// <summary>
/// Provides the parents for the current XAML node in a lazy way.
/// </summary>
/// <remarks>This interface is used by the XAML compiler and shouldn't be implemented in your code.</remarks>
public interface IAvaloniaXamlIlParentStackProvider
{
	/// <summary>
	/// Gets an enumerator iterating over the available parents in the whole hierarchy.
	/// The parents are returned in normal order:
	/// the first element is the most direct parent while the last element is the most distant ancestor.
	/// </summary>
	IEnumerable<object> Parents { get; }
}
