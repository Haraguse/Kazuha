namespace Avalonia.Data;

/// <summary>
/// The priority of a value or binding.
/// </summary>
public enum BindingPriority
{
	/// <summary>
	/// A value that comes from an animation.
	/// </summary>
	Animation = -1,
	/// <summary>
	/// A local value.
	/// </summary>
	LocalValue = 0,
	/// <summary>
	/// A triggered style value.
	/// </summary>
	/// <remarks>
	/// A style trigger is a selector such as .class which overrides a
	/// <see cref="F:Avalonia.Data.BindingPriority.Template" /> value. In this way, a control can have, e.g. a Background from
	/// the template which changes when the control has the :pointerover class.
	/// </remarks>
	StyleTrigger = 1,
	/// <summary>
	/// A value from the control's template.
	/// </summary>
	Template = 2,
	/// <summary>
	/// A style value.
	/// </summary>
	Style = 3,
	/// <summary>
	/// The value is inherited from an ancestor element.
	/// </summary>
	Inherited = 4,
	/// <summary>
	/// The value is uninitialized.
	/// </summary>
	Unset = int.MaxValue
}
