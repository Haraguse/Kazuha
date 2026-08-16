using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the optional arguments to use when calling an implementation of the 
/// <see cref="T:FluentAvalonia.UI.Controls.IFAElementFactory" />'s <see cref="M:FluentAvalonia.UI.Controls.IFAElementFactory.GetElement(FluentAvalonia.UI.Controls.FAElementFactoryGetArgs)" />
/// </summary>
public class FAElementFactoryGetArgs
{
	/// <summary>
	/// Gets or sets teh data item for which an appropriate element tree should be realized when calling 
	/// <see cref="M:FluentAvalonia.UI.Controls.IFAElementFactory.GetElement(FluentAvalonia.UI.Controls.FAElementFactoryGetArgs)" />
	/// </summary>
	public object Data { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Controls.Control" /> that is expected to be the parent of the realized element from
	/// <see cref="M:FluentAvalonia.UI.Controls.IFAElementFactory.GetElement(FluentAvalonia.UI.Controls.FAElementFactoryGetArgs)" />
	/// </summary>
	public Control Parent { get; set; }

	internal int Index { get; set; }
}
