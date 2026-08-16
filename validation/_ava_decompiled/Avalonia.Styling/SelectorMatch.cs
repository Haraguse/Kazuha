using System;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling;

/// <summary>
/// Holds the result of a <see cref="T:Avalonia.Styling.Selector" /> match.
/// </summary>
/// <remarks>
/// A selector match describes whether and how a <see cref="T:Avalonia.Styling.Selector" /> matches a control, and
/// in addition whether the selector can ever match a control of the same type.
/// </remarks>
internal readonly record struct SelectorMatch
{
	/// <summary>
	/// Gets a value indicating whether the match was positive.
	/// </summary>
	public bool IsMatch => Result >= SelectorMatchResult.Sometimes;

	/// <summary>
	/// Gets the result of the match.
	/// </summary>
	public SelectorMatchResult Result { get; }

	/// <summary>
	/// Gets an activator which tracks the selector match, in the case of selectors that can
	/// change over time.
	/// </summary>
	public IStyleActivator? Activator { get; }

	/// <summary>
	/// A selector match with the result of <see cref="F:Avalonia.Styling.SelectorMatchResult.NeverThisType" />.
	/// </summary>
	public static readonly SelectorMatch NeverThisType = new SelectorMatch(SelectorMatchResult.NeverThisType);

	/// <summary>
	/// A selector match with the result of <see cref="F:Avalonia.Styling.SelectorMatchResult.NeverThisInstance" />.
	/// </summary>
	public static readonly SelectorMatch NeverThisInstance = new SelectorMatch(SelectorMatchResult.NeverThisInstance);

	/// <summary>
	/// A selector match with the result of <see cref="F:Avalonia.Styling.SelectorMatchResult.AlwaysThisType" />.
	/// </summary>
	public static readonly SelectorMatch AlwaysThisType = new SelectorMatch(SelectorMatchResult.AlwaysThisType);

	/// <summary>
	/// Gets a selector match with the result of <see cref="F:Avalonia.Styling.SelectorMatchResult.AlwaysThisInstance" />.
	/// </summary>
	public static readonly SelectorMatch AlwaysThisInstance = new SelectorMatch(SelectorMatchResult.AlwaysThisInstance);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.SelectorMatch" /> class with a 
	/// <see cref="F:Avalonia.Styling.SelectorMatchResult.Sometimes" /> result.
	/// </summary>
	/// <param name="match">The match activator.</param>
	public SelectorMatch(IStyleActivator match)
	{
		match = match ?? throw new ArgumentNullException("match");
		Result = SelectorMatchResult.Sometimes;
		Activator = match;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Styling.SelectorMatch" /> class with the specified result.
	/// </summary>
	/// <param name="result">The match result.</param>
	public SelectorMatch(SelectorMatchResult result)
	{
		Result = result;
		Activator = null;
	}

	/// <summary>
	/// Logical ANDs this <see cref="T:Avalonia.Styling.SelectorMatch" /> with another.
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public SelectorMatch And(in SelectorMatch other)
	{
		SelectorMatchResult selectorMatchResult = (SelectorMatchResult)Math.Min((int)Result, (int)other.Result);
		if (selectorMatchResult == SelectorMatchResult.Sometimes)
		{
			AndActivatorBuilder andActivatorBuilder = default(AndActivatorBuilder);
			andActivatorBuilder.Add(Activator);
			andActivatorBuilder.Add(other.Activator);
			return new SelectorMatch(andActivatorBuilder.Get());
		}
		return new SelectorMatch(selectorMatchResult);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return Result.ToString();
	}
}
