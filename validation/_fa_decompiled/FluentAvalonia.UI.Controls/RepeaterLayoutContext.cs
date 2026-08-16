using System;
using Avalonia;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

internal class RepeaterLayoutContext : FAVirtualizingLayoutContext
{
	private WeakReference<FAItemsRepeater> _owner;

	protected internal override object LayoutStateCore
	{
		get
		{
			return GetOwner()?.LayoutState;
		}
		set
		{
			FAItemsRepeater owner = GetOwner();
			if (owner != null)
			{
				owner.LayoutState = value;
			}
		}
	}

	public RepeaterLayoutContext(FAItemsRepeater owner)
	{
		_owner = new WeakReference<FAItemsRepeater>(owner);
	}

	protected internal override int ItemCountCore()
	{
		return (GetOwner()?.ItemsSourceView)?.Count ?? 0;
	}

	protected override Control GetOrCreateElementAtCore(int index, FAElementRealizationOptions options)
	{
		return GetOwner()?.GetElementImpl(index, (options & FAElementRealizationOptions.ForceCreate) == FAElementRealizationOptions.ForceCreate, (options & FAElementRealizationOptions.SuppressAutoRecycle) == FAElementRealizationOptions.SuppressAutoRecycle);
	}

	protected override object GetItemAtCore(int index)
	{
		return GetOwner()?.ItemsSourceView?.GetAt(index);
	}

	protected override void RecycleElementCore(Control element)
	{
		GetOwner()?.ClearElementImpl(element);
	}

	protected override Rect VisibleRectCore()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return (Rect)(((_003F?)GetOwner()?.VisibleWindow) ?? default(Rect));
	}

	protected override Rect RealizationRectCore()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return (Rect)(((_003F?)GetOwner()?.RealizationWindow) ?? default(Rect));
	}

	protected override int RecommendedAnchorIndexCore()
	{
		int result = -1;
		FAItemsRepeater owner = GetOwner();
		Control val = owner?.SuggestedAnchor;
		if (val != null)
		{
			result = owner.GetElementIndex(val);
		}
		return result;
	}

	protected override Point LayoutOriginCore()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return (Point)(((_003F?)GetOwner()?.LayoutOrigin) ?? default(Point));
	}

	protected override void LayoutOriginCore(Point value)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		FAItemsRepeater owner = GetOwner();
		if (owner != null)
		{
			owner.LayoutOrigin = value;
		}
	}

	private FAItemsRepeater GetOwner()
	{
		if (_owner.TryGetTarget(out var target))
		{
			return target;
		}
		return null;
	}
}
