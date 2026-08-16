using System;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

/// <summary>
/// The `:not()` style selector.
/// </summary>
internal class NotSelector : Selector
{
	private readonly Selector? _previous;

	private readonly Selector _argument;

	private string? _selectorString;

	/// <inheritdoc />
	internal override bool InTemplate => _argument.InTemplate;

	/// <inheritdoc />
	internal override bool IsCombinator => false;

	/// <inheritdoc />
	internal override Type? TargetType => _previous?.TargetType;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.NotSelector" /> class.
	/// </summary>
	/// <param name="previous">The previous selector.</param>
	/// <param name="argument">The selector to be not-ed.</param>
	public NotSelector(Selector? previous, Selector argument)
	{
		_previous = previous;
		_argument = argument ?? throw new InvalidOperationException("Not selector must have a selector argument.");
	}

	/// <inheritdoc />
	public override string ToString(Style? owner)
	{
		if (_selectorString == null)
		{
			_selectorString = $"{_previous?.ToString(owner, hasNext: true)}:not({_argument})";
		}
		return _selectorString;
	}

	private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
	{
		SelectorMatch selectorMatch = _argument.Match(control, parent, subscribe);
		return selectorMatch.Result switch
		{
			SelectorMatchResult.AlwaysThisInstance => SelectorMatch.NeverThisInstance, 
			SelectorMatchResult.AlwaysThisType => SelectorMatch.NeverThisType, 
			SelectorMatchResult.NeverThisInstance => SelectorMatch.AlwaysThisInstance, 
			SelectorMatchResult.NeverThisType => SelectorMatch.AlwaysThisType, 
			SelectorMatchResult.Sometimes => new SelectorMatch(new NotActivator(selectorMatch.Activator)), 
			_ => throw new InvalidOperationException("Invalid SelectorMatchResult."), 
		};
	}

	private protected override Selector? MovePrevious()
	{
		return _previous;
	}

	private protected override Selector? MovePreviousOrParent()
	{
		return _previous;
	}
}
