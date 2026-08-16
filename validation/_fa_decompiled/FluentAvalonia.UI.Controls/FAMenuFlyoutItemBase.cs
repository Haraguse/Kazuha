using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace FluentAvalonia.UI.Controls;

public class FAMenuFlyoutItemBase : TemplatedControl
{
	internal bool IsContainerFromTemplate { get; set; }

	internal FAMenuFlyoutPresenter InternalParent { get; set; }

	static FAMenuFlyoutItemBase()
	{
		InputElement.FocusableProperty.OverrideDefaultValue<FAMenuFlyoutItemBase>(true);
	}

	protected override void OnPointerEntered(PointerEventArgs e)
	{
		((InputElement)this).OnPointerEntered(e);
		InternalParent.PointerEnteredItem(this);
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		((InputElement)this).OnPointerExited(e);
		InternalParent.PointerExitedItem(this);
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		((InputElement)this).OnPointerCaptureLost(e);
	}
}
