using System;

namespace Avalonia.Styling;

internal abstract class ValueStyleQuery<T> : StyleQuery
{
	private readonly StyleQuery? _previous;

	private T _argument;

	protected T Argument => _argument;

	internal override bool IsCombinator => false;

	internal ValueStyleQuery(StyleQuery? previous, T argument)
	{
		_previous = previous;
		_argument = argument;
	}

	public override string ToString(ContainerQuery? owner)
	{
		throw new NotImplementedException();
	}

	private protected override StyleQuery? MovePrevious()
	{
		return _previous;
	}

	private protected override StyleQuery? MovePreviousOrParent()
	{
		return _previous;
	}
}
