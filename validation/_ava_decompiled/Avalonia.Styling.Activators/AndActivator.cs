using System.Collections.Generic;

namespace Avalonia.Styling.Activators;

/// <summary>
/// An aggregate <see cref="T:Avalonia.Styling.Activators.IStyleActivator" /> which is active when all of its inputs are
/// active.
/// </summary>
internal class AndActivator : StyleActivatorBase, IStyleActivatorSink
{
	private List<IStyleActivator>? _sources;

	public int Count => _sources?.Count ?? 0;

	public void Add(IStyleActivator activator)
	{
		if (base.IsSubscribed)
		{
			throw new AvaloniaInternalException("AndActivator is already subscribed.");
		}
		if (_sources == null)
		{
			_sources = new List<IStyleActivator>();
		}
		_sources.Add(activator);
	}

	void IStyleActivatorSink.OnNext(bool value)
	{
		ReevaluateIsActive();
	}

	protected override bool EvaluateIsActive()
	{
		if (_sources == null || _sources.Count == 0)
		{
			return true;
		}
		int count = _sources.Count;
		ulong num = (ulong)((1L << count) - 1);
		ulong num2 = 0uL;
		for (int i = 0; i < count; i++)
		{
			if (_sources[i].GetIsActive())
			{
				num2 |= (ulong)(1L << i);
			}
		}
		return num2 == num;
	}

	protected override void Initialize()
	{
		if (_sources == null)
		{
			return;
		}
		foreach (IStyleActivator source in _sources)
		{
			source.Subscribe(this);
		}
	}

	protected override void Deinitialize()
	{
		if (_sources == null)
		{
			return;
		}
		foreach (IStyleActivator source in _sources)
		{
			source.Unsubscribe(this);
		}
	}
}
