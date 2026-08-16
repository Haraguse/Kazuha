using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls.Internal;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the container for an item in a <see cref="T:FluentAvalonia.UI.Controls.FAComboBox" /> control.
/// </summary>
public class FAComboBoxItem : FASelectorItem
{
	static FAComboBoxItem()
	{
		InputElement.FocusableProperty.OverrideDefaultValue<FAComboBoxItem>(true);
	}

	protected override void OnGotFocus(FocusChangedEventArgs e)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		((Control)this).OnGotFocus(e);
		if ((int)e.NavigationMethod == 2 || (int)e.NavigationMethod == 1)
		{
			((((StyledElement)this).Parent as FAComboBox) ?? VisualExtensions.FindAncestorOfType<FAComboBox>((Visual)(object)this, false))?.ItemFocused(this);
		}
	}
}
