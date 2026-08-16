using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositorAnimations
{
	private readonly HashSet<IServerClockItem> _clockItems = new HashSet<IServerClockItem>();

	private readonly List<IServerClockItem> _clockItemsToUpdate = new List<IServerClockItem>();

	private readonly HashSet<ServerObjectAnimations> _dirtyAnimatedObjects = new HashSet<ServerObjectAnimations>();

	private readonly Queue<ServerObjectAnimations> _dirtyAnimatedObjectQueue = new Queue<ServerObjectAnimations>();

	public bool NeedNextTick => _clockItems.Count > 0;

	public void AddToClock(IServerClockItem item)
	{
		_clockItems.Add(item);
	}

	public void RemoveFromClock(IServerClockItem item)
	{
		_clockItems.Remove(item);
	}

	public void Process()
	{
		foreach (IServerClockItem clockItem in _clockItems)
		{
			_clockItemsToUpdate.Add(clockItem);
		}
		foreach (IServerClockItem item in _clockItemsToUpdate)
		{
			item.OnTick();
		}
		_clockItemsToUpdate.Clear();
		while (_dirtyAnimatedObjectQueue.Count > 0)
		{
			ServerObjectAnimations serverObjectAnimations = _dirtyAnimatedObjectQueue.Dequeue();
			_dirtyAnimatedObjects.Remove(serverObjectAnimations);
			serverObjectAnimations.EvaluateAnimations();
		}
		_dirtyAnimatedObjects.Clear();
	}

	public void AddDirtyAnimatedObject(ServerObjectAnimations obj)
	{
		if (_dirtyAnimatedObjects.Add(obj))
		{
			_dirtyAnimatedObjectQueue.Enqueue(obj);
		}
	}
}
