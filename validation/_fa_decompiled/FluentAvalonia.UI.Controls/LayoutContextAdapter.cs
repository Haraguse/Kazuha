using System;
using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

internal class LayoutContextAdapter : FAVirtualizingLayoutContext
{
	private FANonVirtualizingLayoutContext _nonVirtualizingContext;

	protected internal override object LayoutStateCore
	{
		get
		{
			return _nonVirtualizingContext?.LayoutState;
		}
		set
		{
			if (_nonVirtualizingContext != null)
			{
				_nonVirtualizingContext.LayoutState = value;
			}
		}
	}

	public LayoutContextAdapter(FANonVirtualizingLayoutContext nonVirtualizingContext)
	{
		_nonVirtualizingContext = nonVirtualizingContext;
	}

	protected internal override int ItemCountCore()
	{
		return _nonVirtualizingContext?.Children.Count ?? 0;
	}

	protected override object GetItemAtCore(int index)
	{
		return _nonVirtualizingContext?.Children[index] ?? null;
	}

	protected override Control GetOrCreateElementAtCore(int index, FAElementRealizationOptions options)
	{
		if (_nonVirtualizingContext != null)
		{
			return _nonVirtualizingContext.Children[index];
		}
		return null;
	}

	protected override void RecycleElementCore(Control element)
	{
	}

	private int GetElementIndexCore(Control element)
	{
		int result = -1;
		if (_nonVirtualizingContext != null)
		{
			result = _nonVirtualizingContext.Children.IndexOf(element);
		}
		return result;
	}

	protected override Rect VisibleRectCore()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(0.0, 0.0, double.PositiveInfinity, double.PositiveInfinity);
	}

	protected override Rect RealizationRectCore()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(0.0, 0.0, double.PositiveInfinity, double.PositiveInfinity);
	}

	protected override int RecommendedAnchorIndexCore()
	{
		return -1;
	}

	protected override Point LayoutOriginCore()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return default(Point);
	}

	protected override void LayoutOriginCore(Point value)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (value != default(Point))
		{
			throw new ArgumentException("LayoutOrigin must be at (0,0) when RealizationRect is infinite sized.");
		}
	}
}
