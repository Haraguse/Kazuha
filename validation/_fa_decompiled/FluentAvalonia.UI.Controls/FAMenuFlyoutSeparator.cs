using Avalonia.Input;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a horizontal line that separates items in a <see cref="T:FluentAvalonia.UI.Controls.FAMenuFlyout" />
/// </summary>
public class FAMenuFlyoutSeparator : FAMenuFlyoutItemBase
{
	static FAMenuFlyoutSeparator()
	{
		InputElement.FocusableProperty.OverrideDefaultValue<FAMenuFlyoutSeparator>(false);
	}
}
