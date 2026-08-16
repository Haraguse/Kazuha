using System.Collections.Generic;

namespace Avalonia.Styling.Activators;

/// <summary>
/// An aggregate <see cref="T:Avalonia.Styling.Activators.IStyleActivator" /> which is active when any of its inputs are
/// active.
/// </summary>
internal class OrActivator : StyleActivatorBase, IStyleActivatorSink
{
	private List<IStyleActivator>? _sources;

	public int Count => _sources?.Count ?? 0;

	public void Add(IStyleActivator activator)
	{
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
		foreach (IStyleActivator source in _sources)
		{
			if (source.GetIsActive())
			{
				return true;
			}
		}
		return false;
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
