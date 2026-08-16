using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

/// <summary>
/// The OR style query.
/// </summary>
internal sealed class OrQuery : StyleQuery
{
	private readonly IReadOnlyList<StyleQuery> _queries;

	private string? _queryString;

	/// <inheritdoc />
	internal override bool IsCombinator => false;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.OrQuery" /> class.
	/// </summary>
	/// <param name="queries">The querys to OR.</param>
	public OrQuery(IReadOnlyList<StyleQuery> queries)
	{
		if (queries == null)
		{
			throw new ArgumentNullException("queries");
		}
		if (queries.Count <= 1)
		{
			throw new ArgumentException("Need more than one query to OR.");
		}
		_queries = queries;
	}

	/// <inheritdoc />
	public override string ToString(ContainerQuery? owner)
	{
		if (_queryString == null)
		{
			_queryString = string.Join(", ", _queries.Select((StyleQuery x) => x.ToString(owner)));
		}
		return _queryString;
	}

	internal override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe, string? containerName = null)
	{
		if (!(control is Visual visual))
		{
			return SelectorMatch.NeverThisType;
		}
		OrQueryActivatorBuilder orQueryActivatorBuilder = new OrQueryActivatorBuilder(visual);
		bool flag = false;
		int count = _queries.Count;
		for (int i = 0; i < count; i++)
		{
			SelectorMatch result = _queries[i].Match(control, parent, subscribe, containerName);
			switch (result.Result)
			{
			case SelectorMatchResult.AlwaysThisInstance:
			case SelectorMatchResult.AlwaysThisType:
				return result;
			case SelectorMatchResult.NeverThisInstance:
				flag = true;
				break;
			case SelectorMatchResult.Sometimes:
				orQueryActivatorBuilder.Add(result.Activator);
				break;
			}
		}
		if (orQueryActivatorBuilder.Count > 0)
		{
			return new SelectorMatch(orQueryActivatorBuilder.Get());
		}
		if (flag)
		{
			return SelectorMatch.NeverThisInstance;
		}
		return SelectorMatch.NeverThisType;
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
