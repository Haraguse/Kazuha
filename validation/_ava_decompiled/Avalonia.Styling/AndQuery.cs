using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

/// <summary>
/// The AND style query.
/// </summary>
internal sealed class AndQuery : StyleQuery
{
	private readonly IReadOnlyList<StyleQuery> _queries;

	private string? _queryString;

	/// <inheritdoc />
	internal override bool IsCombinator => false;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.AndQuery" /> class.
	/// </summary>
	/// <param name="queries">The queries to AND.</param>
	public AndQuery(IReadOnlyList<StyleQuery> queries)
	{
		if (queries == null)
		{
			throw new ArgumentNullException("queries");
		}
		if (queries.Count <= 1)
		{
			throw new ArgumentException("Need more than one query to AND.");
		}
		_queries = queries;
	}

	/// <inheritdoc />
	public override string ToString(ContainerQuery? owner)
	{
		if (_queryString == null)
		{
			_queryString = string.Join(" and ", _queries.Select((StyleQuery x) => x.ToString(owner)));
		}
		return _queryString;
	}

	internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe, string? containerName = null)
	{
		if (!(control is Visual visual))
		{
			return SelectorMatch.NeverThisType;
		}
		AndQueryActivatorBuilder andQueryActivatorBuilder = new AndQueryActivatorBuilder(visual);
		bool flag = false;
		int count = _queries.Count;
		for (int i = 0; i < count; i++)
		{
			SelectorMatch result = _queries[i].Match(control, parent, subscribe, containerName);
			switch (result.Result)
			{
			case SelectorMatchResult.AlwaysThisInstance:
				flag = true;
				break;
			case SelectorMatchResult.NeverThisType:
			case SelectorMatchResult.NeverThisInstance:
				return result;
			case SelectorMatchResult.Sometimes:
				andQueryActivatorBuilder.Add(result.Activator);
				break;
			}
		}
		if (andQueryActivatorBuilder.Count > 0)
		{
			return new SelectorMatch(andQueryActivatorBuilder.Get());
		}
		if (flag)
		{
			return SelectorMatch.AlwaysThisInstance;
		}
		return SelectorMatch.AlwaysThisType;
	}

	private protected override StyleQuery? MovePrevious()
	{
		return null;
	}

	private protected override StyleQuery? MovePreviousOrParent()
	{
		return null;
	}
}
