using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

/// <summary>
/// A query in a <see cref="T:Avalonia.Styling.ContainerQuery" />.
/// </summary>
public abstract class StyleQuery
{
	/// <summary>
	/// Gets a value indicating whether this query is a combinator.
	/// </summary>
	/// <remarks>
	/// A combinator is a query such as Child or Descendent which links simple querys.
	/// </remarks>
	internal abstract bool IsCombinator { get; }

	internal StyleQuery()
	{
	}

	/// <summary>
	/// Tries to match the query with a control.
	/// </summary>
	/// <param name="control">The control.</param>
	/// <param name="parent">
	/// The parent container, if the container containing the query is a nested container.
	/// </param>
	/// <param name="subscribe">
	/// Whether the match should subscribe to changes in order to track the match over time,
	/// or simply return an imcontainerte result.
	/// </param>
	/// <param name="containerName">
	/// The name of container to query on.
	/// </param>
	/// <returns>A <see cref="T:Avalonia.Styling.SelectorMatch" />.</returns>
	internal virtual SelectorMatch Match(StyledElement control, IStyle? parent = null, bool subscribe = true, string? containerName = null)
	{
		SelectorMatch result = MatchUntilCombinator(control, this, parent, subscribe, out StyleQuery combinator, containerName);
		if (result.IsMatch && combinator != null)
		{
			result = result.And(combinator.Match(control, parent, subscribe, containerName));
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
	/// Gets a string representing the query, with the nesting separator (`^`) replaced with
	/// the parent query.
	/// </summary>
	/// <param name="owner">The owner container.</param>
	public abstract string ToString(ContainerQuery? owner);

	/// <summary>
	/// Evaluates the query for a match.
	/// </summary>
	/// <param name="control">The control.</param>
	/// <param name="parent">
	/// The parent container, if the container containing the query is a nested container.
	/// </param>
	/// <param name="subscribe">
	/// Whether the match should subscribe to changes in order to track the match over time,
	/// or simply return an imcontainerte result.
	/// </param>
	/// <param name="containerName">
	/// The name of the container to evaluate.
	/// </param>
	/// <returns>A <see cref="T:Avalonia.Styling.SelectorMatch" />.</returns>
	internal abstract SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe, string? containerName);

	/// <summary>
	/// Moves to the previous query.
	/// </summary>
	private protected abstract StyleQuery? MovePrevious();

	/// <summary>
	/// Moves to the previous query or the parent query.
	/// </summary>
	private protected abstract StyleQuery? MovePreviousOrParent();

	private static SelectorMatch MatchUntilCombinator(StyledElement control, StyleQuery start, IStyle? parent, bool subscribe, out StyleQuery? combinator, string? containerName = null)
	{
		combinator = null;
		AndActivatorBuilder activators = default(AndActivatorBuilder);
		SelectorMatchResult selectorMatchResult = Match(control, start, parent, subscribe, ref activators, ref combinator, containerName);
		if (selectorMatchResult != SelectorMatchResult.Sometimes)
		{
			return new SelectorMatch(selectorMatchResult);
		}
		return new SelectorMatch(activators.Get());
	}

	private static SelectorMatchResult Match(StyledElement control, StyleQuery query, IStyle? parent, bool subscribe, ref AndActivatorBuilder activators, ref StyleQuery? combinator, string? containerName)
	{
		StyleQuery styleQuery = query.MovePrevious();
		if (styleQuery != null && !styleQuery.IsCombinator)
		{
			SelectorMatchResult selectorMatchResult = Match(control, styleQuery, parent, subscribe, ref activators, ref combinator, containerName);
			if (selectorMatchResult < SelectorMatchResult.Sometimes)
			{
				return selectorMatchResult;
			}
		}
		SelectorMatch selectorMatch = query.Evaluate(control, parent, subscribe, containerName);
		if (!selectorMatch.IsMatch)
		{
			combinator = null;
			return selectorMatch.Result;
		}
		if (selectorMatch.Activator != null)
		{
			activators.Add(selectorMatch.Activator);
		}
		if (styleQuery != null && styleQuery.IsCombinator)
		{
			combinator = styleQuery;
		}
		return selectorMatch.Result;
	}
}
