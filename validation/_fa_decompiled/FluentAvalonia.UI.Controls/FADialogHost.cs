using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Special control to host a <see cref="T:FluentAvalonia.UI.Controls.FAContentDialog" /> or <see cref="T:FluentAvalonia.UI.Controls.FATaskDialog" />
/// </summary>
/// <remarks>
/// This class should generally not be used outside of FluentAvalonia, and is
/// only public for Xaml styling support
/// </remarks>
public class FADialogHost : ContentControl
{
	private IDisposable _rootBoundsWatcher;

	protected override Type StyleKeyOverride => typeof(OverlayPopupHost);

	public FADialogHost()
	{
		((TemplatedControl)this).Background = null;
		((Layoutable)this).HorizontalAlignment = (HorizontalAlignment)2;
		((Layoutable)this).VerticalAlignment = (VerticalAlignment)2;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		((Layoutable)this).MeasureOverride(availableSize);
		TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)this);
		if (topLevel != null)
		{
			return topLevel.ClientSize;
		}
		Control topLevel2 = (Control)(object)TopLevel.GetTopLevel((Visual)(object)this);
		if (topLevel2 != null)
		{
			Rect bounds = ((Visual)topLevel2).Bounds;
			return ((Rect)(ref bounds)).Size;
		}
		return default(Size);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		((Visual)this).OnAttachedToVisualTree(e);
		Control topLevel = (Control)(object)TopLevel.GetTopLevel((Visual)(object)this);
		if (topLevel != null)
		{
			_rootBoundsWatcher = AvaloniaObjectExtensions.GetObservable<Rect>((AvaloniaObject)(object)topLevel, (AvaloniaProperty<Rect>)(object)Visual.BoundsProperty).Subscribe(delegate
			{
				OnRootBoundsChanged();
			});
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		((Visual)this).OnDetachedFromVisualTree(e);
		_rootBoundsWatcher?.Dispose();
		_rootBoundsWatcher = null;
	}

	protected override void OnPointerEntered(PointerEventArgs e)
	{
		((RoutedEventArgs)e).Handled = true;
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		((RoutedEventArgs)e).Handled = true;
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		((RoutedEventArgs)e).Handled = true;
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		((RoutedEventArgs)e).Handled = true;
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		((RoutedEventArgs)e).Handled = true;
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		((RoutedEventArgs)e).Handled = true;
	}

	protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
	{
		((RoutedEventArgs)e).Handled = true;
	}

	private void OnRootBoundsChanged()
	{
		((Layoutable)this).InvalidateMeasure();
	}
}
