using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// The FlyoutPresenter that is used within a <see cref="T:FluentAvalonia.UI.Controls.Primitives.FAPickerFlyoutBase" />
/// </summary>
[PseudoClasses(new string[] { ":acceptdismiss" })]
[TemplatePart("AcceptButton", typeof(Button))]
[TemplatePart("DismissButton", typeof(Button))]
public class FAPickerFlyoutPresenter : ContentControl
{
	private Button _acceptButton;

	private Button _dismissButton;

	private const string s_pcAcceptDismiss = ":acceptdismiss";

	private const string s_tpAcceptButton = "AcceptButton";

	private const string s_tpDismissButton = "DismissButton";

	/// <summary>
	/// Raised when the Confirmed button is tapped indicating the new Color should be applied
	/// </summary>
	public event TypedEventHandler<FAPickerFlyoutPresenter, EventArgs> Confirmed;

	/// <summary>
	/// Raised when the Dismiss button is tapped, indicating the new color should not be applied
	/// </summary>
	public event TypedEventHandler<FAPickerFlyoutPresenter, EventArgs> Dismissed;

	public FAPickerFlyoutPresenter()
	{
		((StyledElement)this).PseudoClasses.Add(":acceptdismiss");
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (_acceptButton != null)
		{
			_acceptButton.Click -= OnAcceptClick;
		}
		if (_dismissButton != null)
		{
			_dismissButton.Click -= OnDismissClick;
		}
		((TemplatedControl)this).OnApplyTemplate(e);
		_acceptButton = NameScopeExtensions.Find<Button>(e.NameScope, "AcceptButton");
		if (_acceptButton != null)
		{
			_acceptButton.Click += OnAcceptClick;
		}
		_dismissButton = NameScopeExtensions.Find<Button>(e.NameScope, "DismissButton");
		if (_dismissButton != null)
		{
			_dismissButton.Click += OnDismissClick;
		}
	}

	protected override bool RegisterContentPresenter(ContentPresenter presenter)
	{
		if (((StyledElement)presenter).Name == "ContentPresenter")
		{
			return true;
		}
		return ((ContentControl)this).RegisterContentPresenter(presenter);
	}

	private void OnDismissClick(object sender, RoutedEventArgs e)
	{
		Dismissed?.Invoke(this, EventArgs.Empty);
	}

	private void OnAcceptClick(object sender, RoutedEventArgs e)
	{
		Confirmed?.Invoke(this, EventArgs.Empty);
	}

	internal void ShowHideButtons(bool show)
	{
		PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":acceptdismiss", show);
	}
}
