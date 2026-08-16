using System;
using System.Diagnostics;
using Avalonia.Diagnostics;
using Avalonia.PropertyStore;

namespace Avalonia.Styling;

/// <summary>
/// Defines a style.
/// </summary>
public class Style : StyleBase
{
	private Selector? _selector;

	/// <summary>
	/// Gets or sets the style's selector.
	/// </summary>
	public Selector? Selector
	{
		get
		{
			return _selector;
		}
		set
		{
			_selector = ValidateSelector(value);
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.Style" /> class.
	/// </summary>
	public Style()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.Style" /> class.
	/// </summary>
	/// <param name="selector">The style selector.</param>
	public Style(Func<Selector?, Selector> selector)
	{
		Selector = selector(null);
	}

	/// <summary>
	/// Returns a string representation of the style.
	/// </summary>
	/// <returns>A string representation of the style.</returns>
	public override string ToString()
	{
		return Selector?.ToString(this) ?? "Style";
	}

	internal override void SetParent(StyleBase? parent)
	{
		if (parent is Style { Selector: not null })
		{
			if (Selector == null)
			{
				throw new InvalidOperationException("Child styles must have a selector.");
			}
			Selector.ValidateNestingSelector(inControlTheme: false);
		}
		else if (parent is ControlTheme)
		{
			if (Selector == null)
			{
				throw new InvalidOperationException("Child styles must have a selector.");
			}
			Selector.ValidateNestingSelector(inControlTheme: true);
		}
		base.SetParent(parent);
	}

	internal SelectorMatchResult TryAttach(StyledElement target, object? host, FrameType type)
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		SelectorMatchResult result = SelectorMatchResult.NeverThisType;
		if (base.HasSettersOrAnimations)
		{
			using Activity activity = Diagnostic.AttachingStyle()?.AddTag("Style", this);
			SelectorMatch selectorMatch = Selector?.Match(target, base.Parent) ?? ((target != host) ? SelectorMatch.NeverThisInstance : ((!(base.Parent is ContainerQuery containerQuery)) ? SelectorMatch.AlwaysThisInstance : (containerQuery.Query?.Match(target, containerQuery.Parent, subscribe: true, containerQuery.Name) ?? SelectorMatch.NeverThisInstance)));
			activity?.AddTag("SelectorResult", selectorMatch.Result);
			if (selectorMatch.IsMatch)
			{
				Attach(target, selectorMatch.Activator, type, !(Selector is OrSelector));
			}
			result = selectorMatch.Result;
		}
		return result;
	}

	private static Selector? ValidateSelector(Selector? selector)
	{
		if (selector is TemplateSelector)
		{
			throw new InvalidOperationException("Invalid selector: Template selector must be followed by control selector.");
		}
		return selector;
	}
}
