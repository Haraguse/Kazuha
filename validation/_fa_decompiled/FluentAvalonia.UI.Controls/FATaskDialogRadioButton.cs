using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a RadioButton in a <see cref="T:FluentAvalonia.UI.Controls.FATaskDialog" />
/// </summary>
public class FATaskDialogRadioButton : FATaskDialogCommand
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATaskDialogRadioButton.IsChecked" /> property
	/// </summary>
	public static readonly StyledProperty<bool?> IsCheckedProperty = ToggleButton.IsCheckedProperty.AddOwner<FATaskDialogRadioButton>((StyledPropertyMetadata<bool?>)null);

	/// <summary>
	/// Gets or sets whether this RadioButton is checked
	/// </summary>
	public bool? IsChecked
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool?>(IsCheckedProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool?>(IsCheckedProperty, value, (BindingPriority)0);
		}
	}
}
