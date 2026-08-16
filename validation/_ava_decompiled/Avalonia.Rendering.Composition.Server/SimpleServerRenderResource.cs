using System;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal class SimpleServerRenderResource : SimpleServerObject, IServerRenderResource, IServerRenderResourceObserver, IDisposable
{
	private bool _pendingInvalidation;

	private bool _disposed;

	private RefCountingSmallDictionary<IServerRenderResourceObserver> _observers;

	public bool IsDisposed => _disposed;

	public SimpleServerRenderResource(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected new void SetValue<T>(CompositionProperty prop, ref T field, T value)
	{
		SetValue(ref field, value);
	}

	protected void SetValue<T>(ref T field, T value)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return;
		}
		if (_disposed)
		{
			field = value;
			return;
		}
		if (field is IServerRenderResource serverRenderResource)
		{
			serverRenderResource.RemoveObserver(this);
		}
		else if (field is IServerRenderResource[] array)
		{
			IServerRenderResource[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i]?.RemoveObserver(this);
			}
		}
		field = value;
		if (field is IServerRenderResource serverRenderResource2)
		{
			serverRenderResource2.AddObserver(this);
		}
		else if (field is IServerRenderResource[] array3)
		{
			IServerRenderResource[] array2 = array3;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].AddObserver(this);
			}
		}
		Invalidated();
	}

	protected void Invalidated()
	{
		if (!_pendingInvalidation)
		{
			_pendingInvalidation = true;
			base.Compositor.EnqueueRenderResourceForInvalidation(this);
			PropertyChanged();
		}
	}

	protected override void ValuesInvalidated()
	{
		Invalidated();
		base.ValuesInvalidated();
	}

	protected void RemoveObserversFromProperty<T>(ref T field)
	{
		(field as IServerRenderResource)?.RemoveObserver(this);
	}

	public virtual void Dispose()
	{
		_disposed = true;
		_observers = default(RefCountingSmallDictionary<IServerRenderResourceObserver>);
	}

	public virtual void DependencyQueuedInvalidate(IServerRenderResource sender)
	{
		base.Compositor.EnqueueRenderResourceForInvalidation(this);
	}

	protected virtual void PropertyChanged()
	{
	}

	public void AddObserver(IServerRenderResourceObserver observer)
	{
		if (!_disposed)
		{
			_observers.Add(observer);
		}
	}

	public void RemoveObserver(IServerRenderResourceObserver observer)
	{
		if (!_disposed)
		{
			_observers.Remove(observer);
		}
	}

	public virtual void QueuedInvalidate()
	{
		_pendingInvalidation = false;
		foreach (KeyValuePair<IServerRenderResourceObserver, int> observer in _observers)
		{
			observer.Key.DependencyQueuedInvalidate(this);
		}
	}
}
