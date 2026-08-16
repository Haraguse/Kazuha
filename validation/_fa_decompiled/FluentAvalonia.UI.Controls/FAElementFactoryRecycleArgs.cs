using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the optional arguments to use when calling an implementation of the
/// <see cref="T:FluentAvalonia.UI.Controls.IFAElementFactory" />'s <see cref="M:FluentAvalonia.UI.Controls.IFAElementFactory.RecycleElement(FluentAvalonia.UI.Controls.FAElementFactoryRecycleArgs)" />
/// </summary>
public class FAElementFactoryRecycleArgs
{
	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Controls.Control" /> object to recycle when calling
	/// <see cref="M:FluentAvalonia.UI.Controls.IFAElementFactory.RecycleElement(FluentAvalonia.UI.Controls.FAElementFactoryRecycleArgs)" />
	/// </summary>
	public Control Element { get; set; }

	/// <summary>
	/// Gets or sets a reference to the current parent <see cref="T:Avalonia.Controls.Control" /> of the element being recycled
	/// </summary>
	public Control Parent { get; set; }
}
