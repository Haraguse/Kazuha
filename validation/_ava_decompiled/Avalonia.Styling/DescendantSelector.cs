using System;
using Avalonia.LogicalTree;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

internal class DescendantSelector : Selector
{
	private readonly Selector _parent;

	private string? _selectorString;

	/// <inheritdoc />
	internal override bool IsCombinator => true;

	/// <inheritdoc />
	internal override bool InTemplate => _parent.InTemplate;

	/// <inheritdoc />
	internal override Type? TargetType => null;

	public DescendantSelector(Selector? parent)
	{
		_parent = parent ?? throw new InvalidOperationException("Descendant selector must be preceded by a selector.");
	}

	public override string ToString(Style? owner)
	{
		if (_selectorString == null)
		{
			_selectorString = _parent.ToString(owner, hasNext: true) + " ";
		}
		return _selectorString;
	}

	private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
	{
		ILogical logical = control;
		OrActivatorBuilder orActivatorBuilder = default(OrActivatorBuilder);
		while (logical != null)
		{
			logical = logical.LogicalParent;
			if (logical is StyledElement control2)
			{
				SelectorMatch selectorMatch = _parent.Match(control2, parent, subscribe);
				if (selectorMatch.Result == SelectorMatchResult.Sometimes)
				{
					orActivatorBuilder.Add(selectorMatch.Activator);
				}
				else if (selectorMatch.IsMatch)
				{
					return SelectorMatch.AlwaysThisInstance;
				}
			}
		}
		if (orActivatorBuilder.Count > 0)
		{
			return new SelectorMatch(orActivatorBuilder.Get());
		}
		return SelectorMatch.NeverThisInstance;
	}

	private protected override Selector? MovePrevious()
	{
		return null;
	}

	private protected override Selector? MovePreviousOrParent()
	{
		return _parent;
	}
}
