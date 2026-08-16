using System;
using System.Text;
using Avalonia.LogicalTree;
using Avalonia.Styling.Activators;
using Avalonia.Utilities;

namespace Avalonia.Styling;

/// <summary>
/// The :nth-child() pseudo-class matches elements based on their position in a group of siblings.
/// </summary>
/// <remarks>
/// Element indices are 1-based.
/// </remarks>
internal class NthChildSelector : Selector
{
	private const string NthChildSelectorName = "nth-child";

	private const string NthLastChildSelectorName = "nth-last-child";

	private readonly Selector? _previous;

	private readonly bool _reversed;

	internal override bool InTemplate => _previous?.InTemplate ?? false;

	internal override bool IsCombinator => false;

	internal override Type? TargetType => _previous?.TargetType;

	public int Step { get; }

	public int Offset { get; }

	protected internal NthChildSelector(Selector? previous, int step, int offset, bool reversed)
	{
		_previous = previous;
		Step = step;
		Offset = offset;
		_reversed = reversed;
	}

	/// <summary>
	/// Creates an instance of <see cref="T:Avalonia.Styling.NthChildSelector" />
	/// </summary>
	/// <param name="previous">Previous selector.</param>
	/// <param name="step">Position step.</param>
	/// <param name="offset">Initial index offset.</param>
	public NthChildSelector(Selector? previous, int step, int offset)
		: this(previous, step, offset, reversed: false)
	{
	}

	private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
	{
		if (control == null)
		{
			return SelectorMatch.NeverThisType;
		}
		if (((ILogical)control).LogicalParent is IChildIndexProvider childIndexProvider)
		{
			if (!subscribe)
			{
				return Evaluate(childIndexProvider.GetChildIndex(control), childIndexProvider, Step, Offset, _reversed);
			}
			return new SelectorMatch(new NthChildActivator(control, childIndexProvider, Step, Offset, _reversed));
		}
		return SelectorMatch.NeverThisInstance;
	}

	internal static SelectorMatch Evaluate(int index, IChildIndexProvider childIndexProvider, int step, int offset, bool reversed)
	{
		if (index < 0)
		{
			return SelectorMatch.NeverThisInstance;
		}
		if (reversed)
		{
			if (!childIndexProvider.TryGetTotalCount(out var count))
			{
				return SelectorMatch.NeverThisInstance;
			}
			index = count - index;
		}
		else
		{
			index++;
		}
		int num = Math.Sign(step);
		int num2 = index - offset;
		if (num2 != 0 && (Math.Sign(num2) != num || num2 % step != 0))
		{
			return SelectorMatch.NeverThisInstance;
		}
		return SelectorMatch.AlwaysThisInstance;
	}

	private protected override Selector? MovePrevious()
	{
		return _previous;
	}

	private protected override Selector? MovePreviousOrParent()
	{
		return _previous;
	}

	public override string ToString(Style? owner)
	{
		StringBuilder stringBuilder = StringBuilderCache.Acquire("nth-last-child".Length + 8);
		stringBuilder.Append(_previous?.ToString(owner, hasNext: true));
		stringBuilder.Append(':');
		stringBuilder.Append(_reversed ? "nth-last-child" : "nth-child");
		stringBuilder.Append('(');
		bool flag = false;
		if (Step != 0)
		{
			flag = true;
			stringBuilder.Append(Step);
			stringBuilder.Append('n');
		}
		if (Offset > 0)
		{
			if (flag)
			{
				stringBuilder.Append('+');
			}
			stringBuilder.Append(Offset);
		}
		else if (Offset < 0)
		{
			stringBuilder.Append('-');
			stringBuilder.Append(-Offset);
		}
		stringBuilder.Append(')');
		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}
}
