using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Layout;

/// <summary>
/// Manages measuring and arranging of controls.
/// </summary>
internal class LayoutManager : ILayoutManager, IDisposable
{
	private class EffectiveViewportChangedListener
	{
		public Layoutable Listener { get; }

		public Rect? Viewport { get; set; }

		public EffectiveViewportChangedListener(Layoutable listener)
		{
			Listener = listener;
		}
	}

	private enum ArrangeResult
	{
		Arranged,
		NotVisible,
		AncestorMeasureInvalid
	}

	private const int MaxPasses = 10;

	private readonly ILayoutRoot _owner;

	private readonly LayoutQueue<Layoutable> _toMeasure = new LayoutQueue<Layoutable>((Layoutable v) => !v.IsMeasureValid);

	private readonly LayoutQueue<Layoutable> _toArrange = new LayoutQueue<Layoutable>((Layoutable v) => !v.IsArrangeValid);

	private readonly List<Layoutable> _toArrangeAfterMeasure = new List<Layoutable>();

	private List<EffectiveViewportChangedListener>? _effectiveViewportChangedListeners;

	private bool _disposed;

	private bool _queued;

	private bool _running;

	private int _totalPassCount;

	private readonly Action _invokeOnRender;

	internal Action<LayoutPassTiming>? LayoutPassTimed { get; set; }

	public virtual event EventHandler? LayoutUpdated;

	public LayoutManager(ILayoutRoot owner)
	{
		_owner = owner;
		_invokeOnRender = ExecuteQueuedLayoutPass;
	}

	/// <inheritdoc />
	public virtual void InvalidateMeasure(Layoutable control)
	{
		control = control ?? throw new ArgumentNullException("control");
		Dispatcher.UIThread.VerifyAccess();
		if (!_disposed && control.IsAttachedToVisualTree)
		{
			if (control.GetLayoutRoot() != _owner)
			{
				throw new ArgumentException("Attempt to call InvalidateMeasure on wrong LayoutManager.");
			}
			_toMeasure.Enqueue(control);
			QueueLayoutPass();
		}
	}

	/// <inheritdoc />
	public virtual void InvalidateArrange(Layoutable control)
	{
		control = control ?? throw new ArgumentNullException("control");
		Dispatcher.UIThread.VerifyAccess();
		if (!_disposed && control.IsAttachedToVisualTree)
		{
			if (control.GetLayoutRoot() != _owner)
			{
				throw new ArgumentException("Attempt to call InvalidateArrange on wrong LayoutManager.");
			}
			_toArrange.Enqueue(control);
			QueueLayoutPass();
		}
	}

	internal void ExecuteQueuedLayoutPass()
	{
		if (_queued)
		{
			ExecuteLayoutPass();
		}
	}

	/// <inheritdoc />
	public virtual void ExecuteLayoutPass()
	{
		Dispatcher.UIThread.VerifyAccess();
		if (_disposed)
		{
			return;
		}
		if (!_running)
		{
			bool flag = LayoutPassTimed != null || Logger.IsEnabled(LogEventLevel.Information, "Layout");
			long startingTimestamp = 0L;
			if (flag)
			{
				Logger.TryGet(LogEventLevel.Information, "Layout")?.Log(this, "Started layout pass. To measure: {Measure} To arrange: {Arrange}", _toMeasure.Count, _toArrange.Count);
				startingTimestamp = Stopwatch.GetTimestamp();
			}
			_toMeasure.BeginLoop(10);
			_toArrange.BeginLoop(10);
			try
			{
				_running = true;
				_totalPassCount++;
				for (int i = 0; i < 10; i++)
				{
					InnerLayoutPass();
					if (!RaiseEffectiveViewportChanged())
					{
						break;
					}
				}
			}
			finally
			{
				_running = false;
			}
			_toMeasure.EndLoop();
			_toArrange.EndLoop();
			if (flag)
			{
				TimeSpan elapsedTime = StopwatchHelper.GetElapsedTime(startingTimestamp);
				LayoutPassTimed?.Invoke(new LayoutPassTiming(_totalPassCount, elapsedTime));
				Logger.TryGet(LogEventLevel.Information, "Layout")?.Log(this, "Layout pass finished in {Time}", elapsedTime);
			}
		}
		_queued = false;
		LayoutUpdated?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc />
	public virtual void ExecuteInitialLayoutPass()
	{
		if (_disposed)
		{
			return;
		}
		try
		{
			if (_owner?.RootVisual == null)
			{
				return;
			}
			Layoutable rootVisual = _owner.RootVisual;
			_running = true;
			Measure(rootVisual);
			Arrange(rootVisual);
		}
		finally
		{
			_running = false;
		}
		ExecuteLayoutPass();
	}

	public void Dispose()
	{
		_disposed = true;
		_toMeasure.Dispose();
		_toArrange.Dispose();
	}

	void ILayoutManager.RegisterEffectiveViewportListener(Layoutable control)
	{
		if (_effectiveViewportChangedListeners == null)
		{
			_effectiveViewportChangedListeners = new List<EffectiveViewportChangedListener>();
		}
		_effectiveViewportChangedListeners.Add(new EffectiveViewportChangedListener(control));
	}

	void ILayoutManager.UnregisterEffectiveViewportListener(Layoutable control)
	{
		if (_effectiveViewportChangedListeners == null)
		{
			return;
		}
		for (int num = _effectiveViewportChangedListeners.Count - 1; num >= 0; num--)
		{
			if (_effectiveViewportChangedListeners[num].Listener == control)
			{
				_effectiveViewportChangedListeners.RemoveAt(num);
			}
		}
	}

	private void InnerLayoutPass()
	{
		for (int i = 0; i < 10; i++)
		{
			ExecuteMeasurePass();
			ExecuteArrangePass();
			if (_toMeasure.Count == 0)
			{
				break;
			}
		}
	}

	private void ExecuteMeasurePass()
	{
		using (Diagnostic.BeginLayoutMeasurePass())
		{
			while (_toMeasure.Count > 0)
			{
				Layoutable layoutable = _toMeasure.Dequeue();
				if (!layoutable.IsMeasureValid)
				{
					Measure(layoutable);
				}
				_toArrange.Enqueue(layoutable);
			}
		}
	}

	private void ExecuteArrangePass()
	{
		using (Diagnostic.BeginLayoutArrangePass())
		{
			while (_toArrange.Count > 0)
			{
				Layoutable layoutable = _toArrange.Dequeue();
				if (!layoutable.IsArrangeValid && Arrange(layoutable) == ArrangeResult.AncestorMeasureInvalid)
				{
					_toArrangeAfterMeasure.Add(layoutable);
				}
			}
			foreach (Layoutable item in _toArrangeAfterMeasure)
			{
				InvalidateArrange(item);
			}
			_toArrangeAfterMeasure.Clear();
		}
	}

	private bool Measure(Layoutable control)
	{
		if (!control.IsVisible || !control.IsAttachedToVisualTree)
		{
			return false;
		}
		if (control.VisualParent is Layoutable control2 && !Measure(control2))
		{
			return false;
		}
		if (!control.IsMeasureValid)
		{
			if (control.GetLayoutRoot()?.RootVisual == control)
			{
				control.Measure(Size.Infinity);
			}
			else if (control.PreviousMeasure.HasValue)
			{
				control.Measure(control.PreviousMeasure.Value);
			}
		}
		return true;
	}

	private ArrangeResult Arrange(Layoutable control)
	{
		if (!control.IsVisible || !control.IsAttachedToVisualTree)
		{
			return ArrangeResult.NotVisible;
		}
		if (control.VisualParent is Layoutable control2)
		{
			ArrangeResult arrangeResult = Arrange(control2);
			if (arrangeResult != ArrangeResult.Arranged)
			{
				return arrangeResult;
			}
		}
		if (!control.IsMeasureValid)
		{
			return ArrangeResult.AncestorMeasureInvalid;
		}
		if (!control.IsArrangeValid)
		{
			if (control.GetLayoutRoot()?.RootVisual == control)
			{
				control.Arrange(new Rect(control.DesiredSize));
			}
			else if (control.PreviousArrange.HasValue)
			{
				control.Arrange(control.PreviousArrange.Value);
			}
		}
		return ArrangeResult.Arranged;
	}

	private void QueueLayoutPass()
	{
		if (!_queued && !_running)
		{
			_queued = true;
			MediaContext.Instance.BeginInvokeOnRender(_invokeOnRender);
		}
	}

	private bool RaiseEffectiveViewportChanged()
	{
		int num = _toMeasure.Count + _toArrange.Count;
		if (_effectiveViewportChangedListeners != null)
		{
			int count = _effectiveViewportChangedListeners.Count;
			ArrayPool<EffectiveViewportChangedListener> shared = ArrayPool<EffectiveViewportChangedListener>.Shared;
			EffectiveViewportChangedListener[] array = shared.Rent(count);
			_effectiveViewportChangedListeners.CopyTo(array);
			try
			{
				for (int i = 0; i < count; i++)
				{
					EffectiveViewportChangedListener effectiveViewportChangedListener = array[i];
					if (effectiveViewportChangedListener.Listener.IsAttachedToVisualTree)
					{
						Rect rect = CalculateEffectiveViewport(effectiveViewportChangedListener.Listener);
						Rect value = rect;
						Rect? viewport = effectiveViewportChangedListener.Viewport;
						if (value != viewport)
						{
							effectiveViewportChangedListener.Listener.RaiseEffectiveViewportChanged(new EffectiveViewportChangedEventArgs(rect));
							effectiveViewportChangedListener.Viewport = rect;
						}
					}
				}
			}
			finally
			{
				shared.Return(array, clearArray: true);
			}
		}
		return num != _toMeasure.Count + _toArrange.Count;
	}

	private Rect CalculateEffectiveViewport(Visual control)
	{
		Rect viewport = new Rect(0.0, 0.0, double.PositiveInfinity, double.PositiveInfinity);
		CalculateEffectiveViewport(control, control, ref viewport);
		return viewport;
	}

	private void CalculateEffectiveViewport(Visual target, Visual control, ref Rect viewport)
	{
		if (control.VisualParent != null)
		{
			CalculateEffectiveViewport(target, control.VisualParent, ref viewport);
		}
		else
		{
			viewport = new Rect(control.Bounds.Size);
		}
		if (control != target && control.ClipToBounds)
		{
			viewport = control.Bounds.Intersect(viewport);
		}
		viewport = viewport.Translate(-control.Bounds.Position);
		if (control == target)
		{
			return;
		}
		ITransform renderTransform = control.RenderTransform;
		if (renderTransform != null)
		{
			if (renderTransform.Value.TryInvert(out var inverted))
			{
				Matrix matrix = Matrix.CreateTranslation(control.RenderTransformOrigin.ToPixels(control.Bounds.Size));
				viewport = viewport.TransformToAABB(-matrix * inverted * matrix);
			}
			else
			{
				viewport = default(Rect);
			}
		}
	}
}
