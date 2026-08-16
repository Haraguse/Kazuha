using Avalonia.LogicalTree;

namespace Avalonia.Styling.Activators;

/// <summary>
/// An <see cref="T:Avalonia.Styling.Activators.IStyleActivator" /> which is active when control's index was changed.
/// </summary>
internal sealed class NthChildActivator : StyleActivatorBase
{
	private readonly ILogical _control;

	private readonly IChildIndexProvider _provider;

	private readonly int _step;

	private readonly int _offset;

	private readonly bool _reversed;

	private int _index = -1;

	public NthChildActivator(ILogical control, IChildIndexProvider provider, int step, int offset, bool reversed)
	{
		_control = control;
		_provider = provider;
		_step = step;
		_offset = offset;
		_reversed = reversed;
	}

	protected override bool EvaluateIsActive()
	{
		return NthChildSelector.Evaluate((_index >= 0) ? _index : _provider.GetChildIndex(_control), _provider, _step, _offset, _reversed).IsMatch;
	}

	protected override void Initialize()
	{
		_provider.ChildIndexChanged += ChildIndexChanged;
	}

	protected override void Deinitialize()
	{
		_provider.ChildIndexChanged -= ChildIndexChanged;
	}

	private void ChildIndexChanged(object? sender, ChildIndexChangedEventArgs e)
	{
		switch (e.Action)
		{
		case ChildIndexChangedAction.ChildIndexChanged:
			if (e.Child == _control)
			{
				_index = ((e.Index >= 0) ? e.Index : _provider.GetChildIndex(_control));
				ReevaluateIsActive();
			}
			break;
		case ChildIndexChangedAction.TotalCountChanged:
			if (!_reversed)
			{
				break;
			}
			goto case ChildIndexChangedAction.ChildIndexesReset;
		case ChildIndexChangedAction.ChildIndexesReset:
			_index = _provider.GetChildIndex(_control);
			ReevaluateIsActive();
			break;
		}
	}
}
