using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

internal struct CompositorResourceHolder<T> where T : SimpleServerObject
{
	private InlineDictionary<Compositor, CompositorRefCountableResource<T>> _dictionary;

	public bool IsAttached => _dictionary.HasEntries;

	public bool CreateOrAddRef(Compositor compositor, ICompositorSerializable? owner, out T resource, Func<Compositor, T> factory)
	{
		if (_dictionary.TryGetValue(compositor, out CompositorRefCountableResource<T> value))
		{
			value.AddRef();
			resource = value.Value;
			return false;
		}
		resource = factory(compositor);
		_dictionary.Add(compositor, new CompositorRefCountableResource<T>(resource));
		if (owner != null)
		{
			compositor.RegisterForSerialization(owner);
		}
		return true;
	}

	public T? TryGetForCompositor(Compositor compositor)
	{
		if (_dictionary.TryGetValue(compositor, out CompositorRefCountableResource<T> value))
		{
			return value.Value;
		}
		return null;
	}

	public T GetForCompositor(Compositor compositor)
	{
		if (_dictionary.TryGetValue(compositor, out CompositorRefCountableResource<T> value))
		{
			return value.Value;
		}
		ThrowDoesNotExist();
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	private static void ThrowDoesNotExist()
	{
		throw new InvalidOperationException("This resource doesn't exist on that compositor");
	}

	public bool Release(Compositor compositor)
	{
		if (!_dictionary.TryGetValue(compositor, out CompositorRefCountableResource<T> value))
		{
			ThrowDoesNotExist();
		}
		if (value.Release(compositor))
		{
			_dictionary.Remove(compositor);
			return true;
		}
		return false;
	}

	public void ProcessPropertyChangeNotification(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.OldValue is ICompositionRenderResource oldResource)
		{
			TransitiveReleaseAll(oldResource);
		}
		if (change.NewValue is ICompositionRenderResource newResource)
		{
			TransitiveAddRefAll(newResource);
		}
	}

	public void TransitiveReleaseAll(ICompositionRenderResource oldResource)
	{
		foreach (KeyValuePair<Compositor, CompositorRefCountableResource<T>> item in _dictionary)
		{
			oldResource.ReleaseOnCompositor(item.Key);
		}
	}

	public void TransitiveAddRefAll(ICompositionRenderResource newResource)
	{
		foreach (KeyValuePair<Compositor, CompositorRefCountableResource<T>> item in _dictionary)
		{
			newResource.AddRefOnCompositor(item.Key);
		}
	}

	public void RegisterForInvalidationOnAllCompositors(ICompositorSerializable serializable)
	{
		foreach (KeyValuePair<Compositor, CompositorRefCountableResource<T>> item in _dictionary)
		{
			item.Key.RegisterForSerialization(serializable);
		}
	}
}
