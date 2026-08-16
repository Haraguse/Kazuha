using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Styling;

/// <summary>
/// Defines an element that has a <see cref="P:Avalonia.Styling.IStyleHost.Styles" /> collection.
/// </summary>
[NotClientImplementable]
public interface IStyleHost
{
	/// <summary>
	/// Gets a value indicating whether <see cref="P:Avalonia.Styling.IStyleHost.Styles" /> is initialized.
	/// </summary>
	/// <remarks>
	/// The <see cref="P:Avalonia.Styling.IStyleHost.Styles" /> property may be lazily initialized, if so this property
	/// indicates whether it has been initialized.
	/// </remarks>
	bool IsStylesInitialized { get; }

	/// <summary>
	/// Gets the styles for the element.
	/// </summary>
	Styles Styles { get; }

	/// <summary>
	/// Gets the parent style host element.
	/// </summary>
	IStyleHost? StylingParent { get; }

	/// <summary>
	/// Called when styles are added to <see cref="P:Avalonia.Styling.IStyleHost.Styles" /> or a nested styles collection.
	/// </summary>
	/// <param name="styles">The added styles.</param>
	void StylesAdded(IReadOnlyList<IStyle> styles);

	/// <summary>
	/// Called when styles are removed from <see cref="P:Avalonia.Styling.IStyleHost.Styles" /> or a nested styles collection.
	/// </summary>
	/// <param name="styles">The removed styles.</param>
	void StylesRemoved(IReadOnlyList<IStyle> styles);
}
