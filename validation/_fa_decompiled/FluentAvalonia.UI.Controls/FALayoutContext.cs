using Avalonia;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the base class for an object that facilitates communication between an attached layout and its host container.
/// </summary>
public abstract class FALayoutContext : AvaloniaObject
{
	/// <summary>
	/// Gets or sets an object that represents the state of a layout.
	/// </summary>
	public object LayoutState
	{
		get
		{
			return LayoutStateCore;
		}
		set
		{
			LayoutStateCore = value;
		}
	}

	protected internal virtual object LayoutStateCore { get; set; }
}
