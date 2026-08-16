using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls.Primitives;

/// <summary>
/// Represents a button in a TaskDialog
/// </summary>
/// <remarks>
/// This type should not be used directly and is generated automatically
/// by a TaskDialog
/// </remarks>
public class FATaskDialogButtonHost : Button
{
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FATaskDialogButtonHost>((StyledPropertyMetadata<FAIconSource>)null);

	public FAIconSource IconSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconSource>(IconSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAIconSource>(IconSourceProperty, value, (BindingPriority)0);
		}
	}

	protected override void OnClick()
	{
		((Button)this).OnClick();
		if (((StyledElement)this).DataContext is FATaskDialogButton fATaskDialogButton)
		{
			fATaskDialogButton.RaiseClick();
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((Button)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", change.NewValue != null);
		}
	}
}
