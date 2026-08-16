namespace Avalonia.Data;

/// <summary>
/// Defines the mode of a <see cref="T:Avalonia.Data.RelativeSource" /> object.
/// </summary>
public enum RelativeSourceMode
{
	/// <summary>
	/// The binding will be to the control's data context.
	/// </summary>
	DataContext,
	/// <summary>
	/// The binding will be to the control's templated parent.
	/// </summary>
	TemplatedParent,
	/// <summary>
	/// The binding will be to the control itself.
	/// </summary>
	Self,
	/// <summary>
	/// The binding will be to an ancestor of the control in the visual tree.
	/// </summary>
	FindAncestor
}
