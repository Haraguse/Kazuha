using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;

namespace Avalonia.Rendering.Composition.Drawing;

internal struct RenderDataResources : IDisposable
{
	public const int NullHandle = -1;

	private PooledList<object?>? _resources;

	private Dictionary<object, int>? _internMap;

	public int Count => _resources?.Count ?? 0;

	public object? this[int handle]
	{
		get
		{
			if (handle != -1)
			{
				return _resources[handle];
			}
			return null;
		}
	}

	public int Intern(object? resource)
	{
		if (resource == null)
		{
			return -1;
		}
		if (_resources == null)
		{
			_resources = new PooledList<object>();
		}
		if (_internMap == null)
		{
			_internMap = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
		}
		if (_internMap.TryGetValue(resource, out var value))
		{
			return value;
		}
		value = _resources.Count;
		_resources.Add(resource);
		_internMap.Add(resource, value);
		return value;
	}

	public int AppendDeserialized(object? resource)
	{
		if (resource == null)
		{
			return -1;
		}
		if (_resources == null)
		{
			_resources = new PooledList<object>();
		}
		int count = _resources.Count;
		_resources.Add(resource);
		return count;
	}

	public void Dispose()
	{
		_resources?.Dispose();
		_resources = null;
		_internMap = null;
	}
}
