namespace Avalonia.Styling;

/// <summary>
/// Describes how a <see cref="T:Avalonia.Styling.SelectorMatch" /> matches a control and its type.
/// </summary>
internal enum SelectorMatchResult
{
	/// <summary>
	/// The selector never matches this type.
	/// </summary>
	NeverThisType,
	/// <summary>
	/// The selector never matches this instance, but can match this type.
	/// </summary>
	NeverThisInstance,
	/// <summary>
	/// The selector matches this instance based on the <see cref="P:Avalonia.Styling.SelectorMatch.Activator" />.
	/// </summary>
	Sometimes,
	/// <summary>
	/// The selector always matches this instance, but doesn't always match this type.
	/// </summary>
	AlwaysThisInstance,
	/// <summary>
	/// The selector always matches this type.
	/// </summary>
	AlwaysThisType
}
