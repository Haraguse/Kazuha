using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Input.TextInput;

internal class TransformTrackingHelper : IDisposable
{
	private readonly bool _deferAfterRenderPass;

	private Visual? _visual;

	private bool _queuedForUpdate;

	private readonly EventHandler<AvaloniaPropertyChangedEventArgs> _propertyChangedHandler;

	private readonly List<Visual> _propertyChangedSubscriptions = new List<Visual>();

	public Matrix? Matrix { get; private set; }

	public event Action? MatrixChanged;

	public TransformTrackingHelper(bool deferAfterRenderPass)
	{
		_deferAfterRenderPass = deferAfterRenderPass;
		_propertyChangedHandler = PropertyChangedHandler;
	}

	public void SetVisual(Visual? visual)
	{
		Dispose();
		_visual = visual;
		if (visual != null)
		{
			visual.AttachedToVisualTree += OnAttachedToVisualTree;
			visual.DetachedFromVisualTree += OnDetachedFromVisualTree;
			if (visual.IsAttachedToVisualTree)
			{
				SubscribeToParents();
			}
			UpdateMatrix();
		}
	}

	public void Dispose()
	{
		if (_visual != null)
		{
			UnsubscribeFromParents();
			_visual.AttachedToVisualTree -= OnAttachedToVisualTree;
			_visual.DetachedFromVisualTree -= OnDetachedFromVisualTree;
			_visual = null;
		}
	}

	private void SubscribeToParents()
	{
		for (Visual visual = _visual; visual != null; visual = visual.VisualParent)
		{
			if (visual != null)
			{
				Visual visual2 = visual;
				visual2.PropertyChanged += _propertyChangedHandler;
				_propertyChangedSubscriptions.Add(visual2);
			}
		}
	}

	private void UnsubscribeFromParents()
	{
		foreach (Visual propertyChangedSubscription in _propertyChangedSubscriptions)
		{
			propertyChangedSubscription.PropertyChanged -= _propertyChangedHandler;
		}
		_propertyChangedSubscriptions.Clear();
	}

	private void UpdateMatrix()
	{
		_queuedForUpdate = false;
		Matrix? matrix = null;
		if (_visual != null && _visual.VisualRoot != null)
		{
			matrix = _visual.TransformToVisual(_visual.VisualRoot);
		}
		if (Matrix != matrix)
		{
			Matrix = matrix;
			MatrixChanged?.Invoke();
		}
	}

	private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs visualTreeAttachmentEventArgs)
	{
		SubscribeToParents();
		UpdateMatrix();
	}

	private void EnqueueForUpdate()
	{
		if (!_queuedForUpdate)
		{
			_queuedForUpdate = true;
			if (_deferAfterRenderPass)
			{
				Dispatcher.UIThread.Post(UpdateMatrix, DispatcherPriority.AfterRender);
			}
			else
			{
				MediaContext.Instance.BeginInvokeOnRender(UpdateMatrix);
			}
		}
	}

	private void PropertyChangedHandler(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.IsEffectiveValueChange && e.Property == Visual.BoundsProperty)
		{
			EnqueueForUpdate();
		}
	}

	private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs visualTreeAttachmentEventArgs)
	{
		UnsubscribeFromParents();
		UpdateMatrix();
	}

	public static IDisposable Track(Visual visual, bool deferAfterRenderPass, Action<Visual, Matrix?> cb)
	{
		TransformTrackingHelper rv = new TransformTrackingHelper(deferAfterRenderPass);
		rv.MatrixChanged += delegate
		{
			cb(visual, rv.Matrix);
		};
		rv.SetVisual(visual);
		return rv;
	}

	public static IObservable<Matrix?> Observe(Visual visual, bool deferAfterRenderPass)
	{
		return Observable.Create(delegate(IObserver<Matrix?> observer)
		{
			TransformTrackingHelper rv = new TransformTrackingHelper(deferAfterRenderPass);
			rv.MatrixChanged += delegate
			{
				observer.OnNext(rv.Matrix);
			};
			rv.SetVisual(visual);
			return rv;
		});
	}
}
