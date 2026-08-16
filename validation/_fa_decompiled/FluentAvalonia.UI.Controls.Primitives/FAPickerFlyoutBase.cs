using System.ComponentModel;
using Avalonia.Controls.Primitives;

namespace FluentAvalonia.UI.Controls.Primitives;

/// <summary>
/// The base class for a Flyout that allows confirming or dismissing
/// </summary>
public abstract class FAPickerFlyoutBase : PopupFlyoutBase
{
	/// <summary>
	/// Provides logic that should performed when the confirmed button is tapped
	/// </summary>
	protected abstract void OnConfirmed();

	/// <summary>
	/// Determines if the Accept and Dismiss buttons should be shown
	/// </summary>
	protected virtual bool ShouldShowConfirmationButtons()
	{
		return true;
	}

	protected override void OnOpening(CancelEventArgs args)
	{
		((PopupFlyoutBase)this).OnOpening(args);
		if (((PopupFlyoutBase)this).Popup.Child is FAPickerFlyoutPresenter fAPickerFlyoutPresenter)
		{
			fAPickerFlyoutPresenter.ShowHideButtons(ShouldShowConfirmationButtons());
		}
	}
}
