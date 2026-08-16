using System;
using Avalonia.PropertyStore;

namespace Avalonia.Styling;

/// <summary>
/// Defines a container.
/// </summary>
public class ContainerQuery : StyleBase
{
	private StyleQuery? _query;

	private string? _name;

	/// <summary>
	/// Gets or sets the container's query.
	/// </summary>
	public StyleQuery? Query
	{
		get
		{
			return _query;
		}
		set
		{
			_query = value;
		}
	}

	/// <summary>
	/// Gets or sets the container's name.
	/// </summary>
	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.ContainerQuery" /> class.
	/// </summary>
	public ContainerQuery()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.ContainerQuery" /> class.
	/// </summary>
	/// <param name="query">The container selector.</param>
	/// <param name="containerName"></param>
	public ContainerQuery(Func<StyleQuery?, StyleQuery> query, string? containerName = null)
	{
		Query = query(null);
		_name = containerName;
	}

	/// <summary>
	/// Returns a string representation of the container.
	/// </summary>
	/// <returns>A string representation of the container.</returns>
	public override string ToString()
	{
		return Query?.ToString(this) ?? "ContainerQuery";
	}

	internal override void SetParent(StyleBase? parent)
	{
		if (parent is ControlTheme)
		{
			base.SetParent(parent);
			return;
		}
		throw new InvalidOperationException("Container cannot be added as a nested style.");
	}

	internal SelectorMatchResult TryAttach(StyledElement target, object? host, FrameType type)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		SelectorMatchResult result = SelectorMatchResult.NeverThisType;
		if (base.HasChildren)
		{
			SelectorMatch selectorMatch = Query?.Match(target, base.Parent, subscribe: true, Name) ?? ((target == host) ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance);
			if (selectorMatch.IsMatch)
			{
				Attach(target, selectorMatch.Activator, type, canShareInstance: true);
			}
			result = selectorMatch.Result;
		}
		return result;
	}
}
