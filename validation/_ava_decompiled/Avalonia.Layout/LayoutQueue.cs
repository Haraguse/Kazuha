using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Logging;

namespace Avalonia.Layout;

internal class LayoutQueue<T> : IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, IDisposable where T : notnull
{
	private struct Info
	{
		public bool Active;

		public int Count;
	}

	private readonly Func<T, bool> _shouldEnqueue;

	private readonly Queue<T> _inner = new Queue<T>();

	private readonly Dictionary<T, Info> _loopQueueInfo = new Dictionary<T, Info>();

	private readonly List<KeyValuePair<T, Info>> _notFinalizedBuffer = new List<KeyValuePair<T, Info>>();

	private int _maxEnqueueCountPerLoop = 1;

	public int Count => _inner.Count;

	public LayoutQueue(Func<T, bool> shouldEnqueue)
	{
		_shouldEnqueue = shouldEnqueue;
	}

	public IEnumerator<T> GetEnumerator()
	{
		return ((IEnumerable<T>)_inner).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _inner.GetEnumerator();
	}

	public T Dequeue()
	{
		T val = _inner.Dequeue();
		if (_loopQueueInfo.TryGetValue(val, out var value))
		{
			value.Active = false;
			_loopQueueInfo[val] = value;
		}
		return val;
	}

	public void Enqueue(T item)
	{
		_loopQueueInfo.TryGetValue(item, out var value);
		if (!value.Active)
		{
			if (value.Count < _maxEnqueueCountPerLoop)
			{
				_inner.Enqueue(item);
				_loopQueueInfo[item] = new Info
				{
					Active = true,
					Count = value.Count + 1
				};
			}
			else
			{
				Logger.TryGet(LogEventLevel.Warning, "Layout")?.Log(this, "Layout cycle detected. Item {Item} was enqueued {Count} times.", item, value.Count);
			}
		}
	}

	public void BeginLoop(int maxEnqueueCountPerLoop)
	{
		_maxEnqueueCountPerLoop = maxEnqueueCountPerLoop;
	}

	public void EndLoop()
	{
		foreach (KeyValuePair<T, Info> item in _loopQueueInfo)
		{
			if (item.Value.Count >= _maxEnqueueCountPerLoop)
			{
				_notFinalizedBuffer.Add(item);
			}
		}
		_loopQueueInfo.Clear();
		foreach (KeyValuePair<T, Info> item2 in _notFinalizedBuffer)
		{
			if (_shouldEnqueue(item2.Key))
			{
				_loopQueueInfo[item2.Key] = new Info
				{
					Active = true,
					Count = 0
				};
				_inner.Enqueue(item2.Key);
			}
		}
		_notFinalizedBuffer.Clear();
	}

	public void Dispose()
	{
		_inner.Clear();
		_loopQueueInfo.Clear();
		_notFinalizedBuffer.Clear();
	}
}
