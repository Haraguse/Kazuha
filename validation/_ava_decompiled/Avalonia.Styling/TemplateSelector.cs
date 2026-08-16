using System;

namespace Avalonia.Styling;

internal class TemplateSelector : Selector
{
	private readonly Selector _parent;

	private string? _selectorString;

	/// <inheritdoc />
	internal override bool InTemplate => true;

	/// <inheritdoc />
	internal override bool IsCombinator => true;

	/// <inheritdoc />
	internal override Type? TargetType => null;

	public TemplateSelector(Selector parent)
	{
		if (parent == null)
		{
			throw new InvalidOperationException("Template selector must be preceeded by a selector.");
		}
		_parent = parent;
	}

	public override string ToString(Style? owner)
	{
		if (_selectorString == null)
		{
			_selectorString = _parent.ToString(owner, hasNext: true) + " /template/ ";
		}
		return _selectorString;
	}

	private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
	{
		if (!(control.TemplatedParent is StyledElement control2))
		{
			return SelectorMatch.NeverThisInstance;
		}
		return _parent.Match(control2, parent, subscribe);
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
