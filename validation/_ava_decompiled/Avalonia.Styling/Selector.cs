using System;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

/// <summary>
/// A selector in a <see cref="T:Avalonia.Styling.Style" />.
/// </summary>
public abstract class Selector
{
	/// <summary>
	/// Gets a value indicating whether either this selector or a previous selector has moved
	/// into a template.
	/// </summary>
	internal abstract bool InTemplate { get; }

	/// <summary>
	/// Gets a value indicating whether this selector is a combinator.
	/// </summary>
	/// <remarks>
	/// A combinator is a selector such as Child or Descendent which links simple selectors.
	/// </remarks>
	internal abstract bool IsCombinator { get; }

	/// <summary>
	/// Gets the target type of the selector, if available.
	/// </summary>
	internal abstract Type? TargetType { get; }

	/// <summary>
	/// Tries to match the selector with a control.
	/// </summary>
	/// <param name="control">The control.</param>
	/// <param name="parent">
	/// The parent style, if the style containing the selector is a nested style.
	/// </param>
	/// <param name="subscribe">
	/// Whether the match should subscribe to changes in order to track the match over time,
	/// or simply return an immediate result.
	/// </param>
	/// <returns>A <see cref="T:Avalonia.Styling.SelectorMatch" />.</returns>
	internal SelectorMatch Match(StyledElement control, IStyle? parent = null, bool subscribe = true)
	{
		SelectorMatch result = MatchUntilCombinator(control, this, parent, subscribe, out Selector combinator);
		if (result.IsMatch && combinator != null)
		{
			result = result.And(combinator.Match(control, parent, subscribe));
			result = result.Result switch
			{
				SelectorMatchResult.AlwaysThisType => SelectorMatch.AlwaysThisInstance, 
				SelectorMatchResult.NeverThisType => SelectorMatch.NeverThisInstance, 
				_ => result, 
			};
		}
		return result;
	}

	public override string ToString()
	{
		return ToString(null);
	}

	/// <summary>
	/// Gets a string representing the selector, with the nesting separator (`^`) replaced with
	/// the parent selector.
	/// </summary>
	/// <param name="owner">The owner style.</param>
	public abstract string ToString(Style? owner);

	/// <summary>
	/// Gets a string representing the selector, with the nesting separator (`^`) replaced with
	/// the parent selector.
	/// </summary>
	/// <param name="owner">The owner style.</param>
	/// <param name="hasNext">Whether there is a selector that comes after this one.</param>
	internal virtual string ToString(Style? owner, bool hasNext)
	{
		return ToString(owner);
	}

	/// <summary>
	/// Evaluates the selector for a match.
	/// </summary>
	/// <param name="control">The control.</param>
	/// <param name="parent">
	/// The parent style, if the style containing the selector is a nested style.
	/// </param>
	/// <param name="subscribe">
	/// Whether the match should subscribe to changes in order to track the match over time,
	/// or simply return an immediate result.
	/// </param>
	/// <returns>A <see cref="T:Avalonia.Styling.SelectorMatch" />.</returns>
	private protected abstract SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe);

	/// <summary>
	/// Moves to the previous selector.
	/// </summary>
	private protected abstract Selector? MovePrevious();

	/// <summary>
	/// Moves to the previous selector or the parent selector.
	/// </summary>
	private protected abstract Selector? MovePreviousOrParent();

	internal virtual void ValidateNestingSelector(bool inControlTheme, int templateCount = 0)
	{
		if (inControlTheme)
		{
			if (!InTemplate && IsCombinator)
			{
				throw new InvalidOperationException("ControlTheme style may not directly contain a child or descendent selector.");
			}
			if (this is TemplateSelector && templateCount++ > 0)
			{
				throw new InvalidOperationException("ControlTemplate styles cannot contain multiple template selectors.");
			}
		}
		Selector selector = MovePreviousOrParent();
		if (selector == null)
		{
			if (!(this is NestingSelector))
			{
				throw new InvalidOperationException("Child styles must have a nesting selector.");
			}
		}
		else
		{
			selector.ValidateNestingSelector(inControlTheme, templateCount);
		}
	}

	private static SelectorMatch MatchUntilCombinator(StyledElement control, Selector start, IStyle? parent, bool subscribe, out Selector? combinator)
	{
		combinator = null;
		AndActivatorBuilder activators = default(AndActivatorBuilder);
		SelectorMatchResult selectorMatchResult = Match(control, start, parent, subscribe, ref activators, ref combinator);
		if (selectorMatchResult != SelectorMatchResult.Sometimes)
		{
			return new SelectorMatch(selectorMatchResult);
		}
		return new SelectorMatch(activators.Get());
	}

	private static SelectorMatchResult Match(StyledElement control, Selector selector, IStyle? parent, bool subscribe, ref AndActivatorBuilder activators, ref Selector? combinator)
	{
		Selector selector2 = selector.MovePrevious();
		if (selector2 != null && !selector2.IsCombinator)
		{
			SelectorMatchResult selectorMatchResult = Match(control, selector2, parent, subscribe, ref activators, ref combinator);
			if (selectorMatchResult < SelectorMatchResult.Sometimes)
			{
				return selectorMatchResult;
			}
		}
		SelectorMatch neverThisInstance = SelectorMatch.NeverThisInstance;
		bool flag = false;
		if (parent is ContainerQuery containerQuery)
		{
			neverThisInstance = containerQuery.Query?.Evaluate(control, containerQuery.Parent, subscribe, containerQuery.Name) ?? SelectorMatch.NeverThisInstance;
			if (!neverThisInstance.IsMatch)
			{
				return neverThisInstance.Result;
			}
			flag = neverThisInstance.Result == SelectorMatchResult.Sometimes;
			if (flag)
			{
				activators.Add(neverThisInstance.Activator);
			}
		}
		neverThisInstance = selector.Evaluate(control, parent, subscribe);
		if (!neverThisInstance.IsMatch)
		{
			combinator = null;
			return neverThisInstance.Result;
		}
		if (neverThisInstance.Activator != null)
		{
			activators.Add(neverThisInstance.Activator);
		}
		if (selector2 != null && selector2.IsCombinator)
		{
			combinator = selector2;
		}
		if (!flag)
		{
			return neverThisInstance.Result;
		}
		return SelectorMatchResult.Sometimes;
	}
}
