using System;
using Avalonia.Collections.Pooled;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// Represents the composition output (e. g. a window, embedded control, entire screen)
/// </summary>
internal class CompositionTarget : CompositionObject
{
	private readonly PooledList<CompositionVisual> _hitTestChildCandidates = new PooledList<CompositionVisual>();

	private bool _hitTestChildCandidatesInUse;

	private CompositionTargetChangedFields _changedFieldsOfCompositionTarget;

	private CompositionVisual? _root;

	private bool _isEnabled;

	private RendererDebugOverlays _debugOverlays;

	private LayoutPassTiming _lastLayoutPassTiming;

	private double _scaling;

	private Size _size;

	private CompositionTransparencyLevel _transparencyLevel;

	internal new ServerCompositionTarget Server { get; }

	public CompositionVisual? Root
	{
		get
		{
			return _root;
		}
		set
		{
			bool flag = false;
			if (_root != value)
			{
				OnRootChanging();
				flag = true;
				_root = value;
				_changedFieldsOfCompositionTarget |= CompositionTargetChangedFields.Root;
				RegisterForSerialization();
			}
			_root = value;
			if (flag)
			{
				OnRootChanged();
			}
		}
	}

	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			bool flag = false;
			if (_isEnabled != value)
			{
				flag = true;
				_isEnabled = value;
				_changedFieldsOfCompositionTarget |= CompositionTargetChangedFields.IsEnabled;
				RegisterForSerialization();
			}
			_isEnabled = value;
		}
	}

	public RendererDebugOverlays DebugOverlays
	{
		get
		{
			return _debugOverlays;
		}
		set
		{
			bool flag = false;
			if (_debugOverlays != value)
			{
				flag = true;
				_debugOverlays = value;
				_changedFieldsOfCompositionTarget |= CompositionTargetChangedFields.DebugOverlays;
				RegisterForSerialization();
			}
			_debugOverlays = value;
		}
	}

	internal LayoutPassTiming LastLayoutPassTiming
	{
		get
		{
			return _lastLayoutPassTiming;
		}
		set
		{
			bool flag = false;
			if (_lastLayoutPassTiming != value)
			{
				flag = true;
				_lastLayoutPassTiming = value;
				_changedFieldsOfCompositionTarget |= CompositionTargetChangedFields.LastLayoutPassTiming;
				RegisterForSerialization();
			}
			_lastLayoutPassTiming = value;
		}
	}

	public double Scaling
	{
		get
		{
			return _scaling;
		}
		set
		{
			bool flag = false;
			if (_scaling != value)
			{
				flag = true;
				_scaling = value;
				_changedFieldsOfCompositionTarget |= CompositionTargetChangedFields.Scaling;
				RegisterForSerialization();
			}
			_scaling = value;
		}
	}

	public Size Size
	{
		get
		{
			return _size;
		}
		set
		{
			bool flag = false;
			if (_size != value)
			{
				flag = true;
				_size = value;
				_changedFieldsOfCompositionTarget |= CompositionTargetChangedFields.Size;
				RegisterForSerialization();
			}
			_size = value;
		}
	}

	public CompositionTransparencyLevel TransparencyLevel
	{
		get
		{
			return _transparencyLevel;
		}
		set
		{
			bool flag = false;
			if (_transparencyLevel != value)
			{
				flag = true;
				_transparencyLevel = value;
				_changedFieldsOfCompositionTarget |= CompositionTargetChangedFields.TransparencyLevel;
				RegisterForSerialization();
			}
			_transparencyLevel = value;
		}
	}

	/// <summary>
	/// Attempts to perform a hit-tst
	/// </summary>
	/// <returns></returns>
	public PooledList<CompositionVisual>? TryHitTest(Point point, CompositionVisual? root, Func<CompositionVisual, bool>? filter)
	{
		Server.Compositor.Readback.NextRead();
		if (root == null)
		{
			root = Root;
		}
		if (root == null)
		{
			return null;
		}
		ServerCompositionVisual.ReadbackData readbackData = root.TryGetValidReadback();
		if (readbackData == null)
		{
			return null;
		}
		point = point.Transform(readbackData.Matrix);
		PooledList<CompositionVisual> result = new PooledList<CompositionVisual>();
		HitTestCore(root, point, result, filter);
		return result;
	}

	private PooledList<CompositionVisual> RentHitTestChildCandidates(out bool releaseToField)
	{
		if (!_hitTestChildCandidatesInUse)
		{
			_hitTestChildCandidatesInUse = true;
			releaseToField = true;
			_hitTestChildCandidates.Clear();
			return _hitTestChildCandidates;
		}
		releaseToField = false;
		return new PooledList<CompositionVisual>();
	}

	private void ReleaseHitTestChildCandidates(PooledList<CompositionVisual> candidates, bool releaseToField)
	{
		if (releaseToField)
		{
			candidates.Clear();
			_hitTestChildCandidatesInUse = false;
		}
		else
		{
			candidates.Dispose();
		}
	}

	private void HitTestCore(CompositionVisual visual, Point parentPoint, PooledList<CompositionVisual> result, Func<CompositionVisual, bool>? filter)
	{
		if (HitTestVisual(visual, parentPoint, filter, out var point))
		{
			if (visual is CompositionContainerVisual visual2)
			{
				HitTestChildren(visual2, point, result, filter);
			}
			if (visual.HitTest(point))
			{
				result.Add(visual);
			}
		}
	}

	private void HitTestChildren(CompositionContainerVisual visual, Point point, PooledList<CompositionVisual> result, Func<CompositionVisual, bool>? filter)
	{
		if (visual.Children.Count >= 32)
		{
			PooledList<CompositionVisual> pooledList = RentHitTestChildCandidates(out var releaseToField);
			try
			{
				if (visual.TryQueryHitTestChildren(point, pooledList))
				{
					foreach (CompositionVisual item in pooledList)
					{
						HitTestCore(item, point, result, filter);
					}
					return;
				}
			}
			finally
			{
				ReleaseHitTestChildCandidates(pooledList, releaseToField);
			}
		}
		for (int num = visual.Children.Count - 1; num >= 0; num--)
		{
			HitTestCore(visual.Children[num], point, result, filter);
		}
	}

	private static bool HitTestVisual(CompositionVisual visual, Point parentPoint, Func<CompositionVisual, bool>? filter, out Point point)
	{
		point = default(Point);
		if (!visual.Visible)
		{
			return false;
		}
		if (filter != null && !filter(visual))
		{
			return false;
		}
		ServerCompositionVisual.ReadbackData readbackData = visual.TryGetValidReadback();
		if (readbackData == null)
		{
			return false;
		}
		if (!visual.DisableSubTreeBoundsHitTestOptimization && (!readbackData.TransformedSubtreeBounds.HasValue || !readbackData.TransformedSubtreeBounds.Value.Contains(parentPoint)))
		{
			return false;
		}
		if (!readbackData.Matrix.TryInvert(out var inverted))
		{
			return false;
		}
		point = parentPoint.Transform(inverted);
		if (visual.ClipToBounds && (point.X < 0.0 || point.Y < 0.0 || point.X > visual.Size.X || point.Y > visual.Size.Y))
		{
			return false;
		}
		IGeometryImpl? clip = visual.Clip;
		if (clip != null && !clip.FillContains(point))
		{
			return false;
		}
		return true;
	}

	public CompositionVisual? TryHitTestFirst(Point point, CompositionVisual? root, Func<CompositionVisual, bool>? filter, Func<CompositionVisual, bool>? resultFilter)
	{
		Server.Compositor.Readback.NextRead();
		if (root == null)
		{
			root = Root;
		}
		if (root == null)
		{
			return null;
		}
		ServerCompositionVisual.ReadbackData readbackData = root.TryGetValidReadback();
		if (readbackData == null)
		{
			return null;
		}
		return HitTestFirstCore(root, point.Transform(readbackData.Matrix), filter, resultFilter);
	}

	internal CompositionVisual? HitTestFirstCore(CompositionVisual visual, Point parentPoint, Func<CompositionVisual, bool>? filter, Func<CompositionVisual, bool>? resultFilter)
	{
		if (!HitTestVisual(visual, parentPoint, filter, out var point))
		{
			return null;
		}
		if (visual is CompositionContainerVisual compositionContainerVisual)
		{
			bool flag = false;
			if (compositionContainerVisual.Children.Count >= 32 && compositionContainerVisual.TryQueryFirstHitTestChild(this, point, filter, resultFilter, out CompositionVisual hit))
			{
				flag = true;
				if (hit != null)
				{
					return hit;
				}
			}
			if (!flag)
			{
				for (int num = compositionContainerVisual.Children.Count - 1; num >= 0; num--)
				{
					CompositionVisual compositionVisual = HitTestFirstCore(compositionContainerVisual.Children[num], point, filter, resultFilter);
					if (compositionVisual != null)
					{
						return compositionVisual;
					}
				}
			}
		}
		if (!visual.HitTest(point) || (resultFilter != null && !resultFilter(visual)))
		{
			return null;
		}
		return visual;
	}

	/// <summary>
	/// Registers the composition target for explicit redraw
	/// </summary>
	public void RequestRedraw()
	{
		RegisterForSerialization();
	}

	internal CompositionTarget(Compositor compositor, ServerCompositionTarget server)
		: base(compositor, server)
	{
		Server = server;
		InitializeDefaults();
	}

	private void OnRootChanged()
	{
		if (Root != null)
		{
			Root.Root = this;
		}
	}

	private void OnRootChanging()
	{
		if (Root != null)
		{
			Root.Root = null;
		}
	}

	private void InitializeDefaults()
	{
	}

	private protected override void SerializeChangesCore(BatchStreamWriter writer)
	{
		base.SerializeChangesCore(writer);
		writer.Write(_changedFieldsOfCompositionTarget);
		if ((_changedFieldsOfCompositionTarget & CompositionTargetChangedFields.Root) == CompositionTargetChangedFields.Root)
		{
			writer.WriteObject(_root?.Server);
		}
		if ((_changedFieldsOfCompositionTarget & CompositionTargetChangedFields.IsEnabled) == CompositionTargetChangedFields.IsEnabled)
		{
			writer.Write(_isEnabled);
		}
		if ((_changedFieldsOfCompositionTarget & CompositionTargetChangedFields.DebugOverlays) == CompositionTargetChangedFields.DebugOverlays)
		{
			writer.Write(_debugOverlays);
		}
		if ((_changedFieldsOfCompositionTarget & CompositionTargetChangedFields.LastLayoutPassTiming) == CompositionTargetChangedFields.LastLayoutPassTiming)
		{
			writer.Write(_lastLayoutPassTiming);
		}
		if ((_changedFieldsOfCompositionTarget & CompositionTargetChangedFields.Scaling) == CompositionTargetChangedFields.Scaling)
		{
			writer.Write(_scaling);
		}
		if ((_changedFieldsOfCompositionTarget & CompositionTargetChangedFields.Size) == CompositionTargetChangedFields.Size)
		{
			writer.Write(_size);
		}
		if ((_changedFieldsOfCompositionTarget & CompositionTargetChangedFields.TransparencyLevel) == CompositionTargetChangedFields.TransparencyLevel)
		{
			writer.Write(_transparencyLevel);
		}
		_changedFieldsOfCompositionTarget = (CompositionTargetChangedFields)0;
	}
}
