using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FluentAvalonia.UI.Controls;

internal class ViewportManager
{
	private FAItemsRepeater _owner;

	private bool _ensuredScroller;

	private IScrollAnchorProvider _scroller;

	private Control _makeAnchorElement;

	private bool _isAnchorOutsideRealizedRange;

	private Action _cacheBuildAction;

	private Rect _visibleWindow;

	private Rect _layoutExtent;

	private Point _expectedViewportShift;

	private Point _pendingViewportShift;

	private Point _unshiftableShift;

	private double _maximumHorizontalCacheLength = 2.0;

	private double _maximumVerticalCacheLength = 2.0;

	private double _horizontalCacheBufferPerSide;

	private double _verticalCacheBufferPerSide;

	private bool _managingViewportDisabled;

	private bool _layoutUpdatedRevoker;

	private bool _effectiveViewportChangedRevoker;

	private bool _renderingToken;

	private const double CacheBufferPerSideInflationPixelDelta = 40.0;

	public double HorizontalCacheLength
	{
		get
		{
			return _maximumHorizontalCacheLength;
		}
		set
		{
			ValidateCacheLength(value);
			_maximumHorizontalCacheLength = value;
			ResetCacheBuffer();
		}
	}

	public double VerticalCacheLength
	{
		get
		{
			return _maximumVerticalCacheLength;
		}
		set
		{
			ValidateCacheLength(value);
			_maximumVerticalCacheLength = value;
			ResetCacheBuffer();
		}
	}

	public Control SuggestedAnchor
	{
		get
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Expected O, but got Unknown
			Control val = _makeAnchorElement;
			Control owner = (Control)(object)_owner;
			if (val == null)
			{
				IScrollAnchorProvider scroller = _scroller;
				Control val2 = ((scroller != null) ? scroller.CurrentAnchor : null);
				if (val2 != null)
				{
					Control val3 = val2;
					for (Visual visualParent = VisualExtensions.GetVisualParent((Visual)(object)val3); visualParent != null; visualParent = VisualExtensions.GetVisualParent(visualParent))
					{
						if ((object)visualParent == owner)
						{
							val = val3;
							break;
						}
						val3 = (Control)visualParent;
					}
				}
			}
			return val;
		}
	}

	public Rect LayoutExtent
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _layoutExtent;
		}
	}

	public Control MadeAnchor => _makeAnchorElement;

	private bool HasScroller => _scroller != null;

	public ViewportManager(FAItemsRepeater owner)
	{
		_owner = owner;
	}

	public Rect GetLayoutVisibleWindowDiscardAnchor()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		Rect result = _visibleWindow;
		if (HasScroller)
		{
			Rect val = ((Rect)(ref result)).WithX(((Rect)(ref result)).X + ((Rect)(ref _layoutExtent)).X + ((Point)(ref _expectedViewportShift)).X + ((Point)(ref _unshiftableShift)).X);
			result = ((Rect)(ref val)).WithY(((Rect)(ref result)).Y + ((Rect)(ref _layoutExtent)).Y + ((Point)(ref _expectedViewportShift)).Y + ((Point)(ref _unshiftableShift)).Y);
		}
		return result;
	}

	public Rect GetLayoutVisibleWindow()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		Rect result = _visibleWindow;
		if (_makeAnchorElement != null && _isAnchorOutsideRealizedRange)
		{
			((Rect)(ref result))._002Ector(default(Point), ((Rect)(ref result)).Size);
		}
		else if (HasScroller)
		{
			Rect val = ((Rect)(ref result)).WithX(((Rect)(ref result)).X + ((Rect)(ref _layoutExtent)).X + ((Point)(ref _expectedViewportShift)).X + ((Point)(ref _unshiftableShift)).X);
			result = ((Rect)(ref val)).WithY(((Rect)(ref result)).Y + ((Rect)(ref _layoutExtent)).Y + ((Point)(ref _expectedViewportShift)).Y + ((Point)(ref _unshiftableShift)).Y);
		}
		return result;
	}

	public Rect GetLayoutRealizationWindow()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		Rect layoutVisibleWindow = GetLayoutVisibleWindow();
		if (HasScroller)
		{
			((Rect)(ref layoutVisibleWindow))._002Ector(((Rect)(ref layoutVisibleWindow)).X - _horizontalCacheBufferPerSide, ((Rect)(ref layoutVisibleWindow)).Y - _verticalCacheBufferPerSide, ((Rect)(ref layoutVisibleWindow)).Width + _horizontalCacheBufferPerSide * 2.0, ((Rect)(ref layoutVisibleWindow)).Height + _verticalCacheBufferPerSide * 2.0);
		}
		return layoutVisibleWindow;
	}

	public void SetLayoutExtent(Rect extent)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		_expectedViewportShift = new Point(((Point)(ref _expectedViewportShift)).X + ((Rect)(ref _layoutExtent)).X - ((Rect)(ref extent)).X, ((Point)(ref _expectedViewportShift)).Y + ((Rect)(ref _layoutExtent)).Y - ((Rect)(ref extent)).Y);
		if ((Math.Abs(((Point)(ref _expectedViewportShift)).X) > 1.0 || Math.Abs(((Point)(ref _expectedViewportShift)).Y) > 1.0) && !_layoutUpdatedRevoker)
		{
			_layoutUpdatedRevoker = true;
			((Layoutable)_owner).LayoutUpdated += OnLayoutUpdated;
		}
		_layoutExtent = extent;
		_pendingViewportShift = _expectedViewportShift;
		if (_scroller != null)
		{
			IScrollAnchorProvider scroller = _scroller;
			((Layoutable)((scroller is Control) ? scroller : null)).InvalidateArrange();
		}
	}

	public void OnLayoutChanged(bool isVirtualizing)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		_managingViewportDisabled = !isVirtualizing;
		_layoutExtent = default(Rect);
		_expectedViewportShift = default(Point);
		_pendingViewportShift = default(Point);
		if (_managingViewportDisabled)
		{
			((Layoutable)_owner).EffectiveViewportChanged -= OnEffectiveViewportChanged;
			_effectiveViewportChangedRevoker = false;
		}
		else if (!_effectiveViewportChangedRevoker)
		{
			_effectiveViewportChangedRevoker = true;
			((Layoutable)_owner).EffectiveViewportChanged += OnEffectiveViewportChanged;
		}
		_unshiftableShift = default(Point);
		ResetCacheBuffer();
	}

	public void OnElementPrepared(Control element, VirtualizationInfo vInfo)
	{
		vInfo.CanBeScrollAnchor = true;
		IScrollAnchorProvider scroller = _scroller;
		if (scroller != null)
		{
			scroller.RegisterAnchorCandidate(element);
		}
	}

	public void OnElementCleared(Control element)
	{
		FAItemsRepeater.GetVirtualizationInfo(element).CanBeScrollAnchor = false;
		IScrollAnchorProvider scroller = _scroller;
		if (scroller != null)
		{
			scroller.UnregisterAnchorCandidate(element);
		}
	}

	public void OnOwnerMeasuring()
	{
		EnsureScroller();
	}

	public void OnOwnerArranged()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_expectedViewportShift = default(Point);
		if (!_managingViewportDisabled && HasScroller)
		{
			double num = _maximumHorizontalCacheLength * ((Rect)(ref _visibleWindow)).Width / 2.0;
			double num2 = _maximumVerticalCacheLength * ((Rect)(ref _visibleWindow)).Height / 2.0;
			if (_horizontalCacheBufferPerSide < num || _verticalCacheBufferPerSide < num2)
			{
				_horizontalCacheBufferPerSide += 40.0;
				_verticalCacheBufferPerSide += 40.0;
				_horizontalCacheBufferPerSide = Math.Min(_horizontalCacheBufferPerSide, num);
				_verticalCacheBufferPerSide = Math.Min(_verticalCacheBufferPerSide, num2);
				RegisterCacheBuildWork();
			}
		}
	}

	private void OnLayoutUpdated(object sender, EventArgs e)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		_layoutUpdatedRevoker = false;
		((Layoutable)_owner).LayoutUpdated -= OnLayoutUpdated;
		if (!_managingViewportDisabled && ((Point)(ref _pendingViewportShift)).X != 0.0 && ((Point)(ref _pendingViewportShift)).Y != 0.0)
		{
			_unshiftableShift = new Point(((Point)(ref _unshiftableShift)).X + ((Point)(ref _pendingViewportShift)).X, ((Point)(ref _unshiftableShift)).Y + ((Point)(ref _pendingViewportShift)).Y);
			_pendingViewportShift = default(Point);
			_expectedViewportShift = default(Point);
			TryInvalidateMeasure();
		}
	}

	public void OnMakeAnchor(Control anchor, bool isAnchorOutsideRealizedRange)
	{
		_makeAnchorElement = anchor;
		_isAnchorOutsideRealizedRange = isAnchorOutsideRealizedRange;
	}

	public void OnBringIntoViewRequested(RequestBringIntoViewEventArgs args)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		if (_managingViewportDisabled)
		{
			return;
		}
		_ = _isAnchorOutsideRealizedRange;
		Control immediateChildOfRepeater = GetImmediateChildOfRepeater((Control)args.TargetObject);
		Enumerator<Control> enumerator = ((AvaloniaList<Control>)(object)((Panel)_owner).Children).GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				Control current = enumerator.Current;
				VirtualizationInfo virtualizationInfo = FAItemsRepeater.GetVirtualizationInfo(current);
				if (virtualizationInfo.CanBeScrollAnchor && current != immediateChildOfRepeater)
				{
					IScrollAnchorProvider scroller = _scroller;
					if (scroller != null)
					{
						scroller.UnregisterAnchorCandidate(current);
					}
					virtualizationInfo.CanBeScrollAnchor = false;
				}
			}
		}
		finally
		{
			((IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
		}
		if (!_renderingToken)
		{
			_renderingToken = true;
			Dispatcher.UIThread.Post((Action)OnCompositionTargetRendering, DispatcherPriority.Loaded);
		}
	}

	private Control GetImmediateChildOfRepeater(Control descendant)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		Control result = descendant;
		Control val = (Control)VisualExtensions.GetVisualParent((Visual)(object)descendant);
		while (val != null && (object)val != _owner)
		{
			result = val;
			val = (Control)VisualExtensions.GetVisualParent((Visual)(object)val);
		}
		if (val == null)
		{
			return null;
		}
		return result;
	}

	private void OnCompositionTargetRendering()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		_renderingToken = false;
		_makeAnchorElement = null;
		Enumerator<Control> enumerator = ((AvaloniaList<Control>)(object)((Panel)_owner).Children).GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				Control current = enumerator.Current;
				VirtualizationInfo virtualizationInfo = FAItemsRepeater.GetVirtualizationInfo(current);
				if (!virtualizationInfo.CanBeScrollAnchor && virtualizationInfo.IsRealized && virtualizationInfo.IsHeldByLayout)
				{
					IScrollAnchorProvider scroller = _scroller;
					if (scroller != null)
					{
						scroller.RegisterAnchorCandidate(current);
					}
					virtualizationInfo.CanBeScrollAnchor = true;
				}
			}
		}
		finally
		{
			((IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
		}
	}

	internal void ResetScrollers()
	{
		_scroller = null;
		((Layoutable)_owner).EffectiveViewportChanged -= OnEffectiveViewportChanged;
		_effectiveViewportChangedRevoker = false;
		_ensuredScroller = false;
	}

	private void OnCacheBuildActionCompleted()
	{
		_cacheBuildAction = null;
		if (!_managingViewportDisabled)
		{
			((Layoutable)_owner).InvalidateMeasure();
		}
	}

	private void OnEffectiveViewportChanged(object sender, EffectiveViewportChangedEventArgs args)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		UpdateViewport(args.EffectiveViewport);
		_pendingViewportShift = default(Point);
		_unshiftableShift = default(Point);
		if (_visibleWindow == default(Rect))
		{
			_layoutExtent = default(Rect);
		}
		if (_layoutUpdatedRevoker)
		{
			_layoutUpdatedRevoker = false;
			((Layoutable)_owner).LayoutUpdated -= OnLayoutUpdated;
		}
	}

	private void EnsureScroller()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (_ensuredScroller)
		{
			return;
		}
		ResetScrollers();
		_scroller = VisualExtensions.FindAncestorOfType<IScrollAnchorProvider>((Visual)(object)_owner, false);
		if (!_managingViewportDisabled)
		{
			if (_scroller == null)
			{
				UpdateViewport(default(Rect));
			}
			else
			{
				_effectiveViewportChangedRevoker = true;
				((Layoutable)_owner).EffectiveViewportChanged += OnEffectiveViewportChanged;
			}
		}
		_ensuredScroller = true;
	}

	private void UpdateViewport(Rect viewport)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		Rect visibleWindow = viewport;
		if (0.0 - ((Rect)(ref visibleWindow)).X <= ((Point)(ref FAItemsRepeater.ClearedElementsArrangePosition)).X && 0.0 - ((Rect)(ref visibleWindow)).Y <= ((Point)(ref FAItemsRepeater.ClearedElementsArrangePosition)).Y)
		{
			_visibleWindow = default(Rect);
		}
		else
		{
			_visibleWindow = visibleWindow;
		}
		TryInvalidateMeasure();
	}

	private void ResetCacheBuffer()
	{
		_horizontalCacheBufferPerSide = 0.0;
		_verticalCacheBufferPerSide = 0.0;
		if (!_managingViewportDisabled)
		{
			RegisterCacheBuildWork();
		}
	}

	private void ValidateCacheLength(double cacheLength)
	{
		if (cacheLength < 0.0 || double.IsInfinity(cacheLength) || double.IsNaN(cacheLength))
		{
			throw new Exception("The maximum cache length must be equal or superior to zero.");
		}
	}

	private void RegisterCacheBuildWork()
	{
		if (_owner.Layout != null && _cacheBuildAction == null)
		{
			_cacheBuildAction = delegate
			{
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				Dispatcher.UIThread.Post((Action)OnCacheBuildActionCompleted, DispatcherPriority.Background);
			};
			_cacheBuildAction();
		}
	}

	private void TryInvalidateMeasure()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (_visibleWindow != default(Rect))
		{
			((Layoutable)_owner).InvalidateMeasure();
		}
	}

	private string GetLayoutId()
	{
		return _owner?.Layout?.LayoutId ?? string.Empty;
	}
}
