namespace Avalonia.Media;

/// <summary>
/// Holds a hit test result from a <see cref="T:Avalonia.Media.TextFormatting.TextLayout" />.
/// </summary>
public readonly record struct TextHitTestResult
{
	/// <summary>
	/// Gets the character hit of the hit test result.
	/// </summary>
	public CharacterHit CharacterHit { get; }

	/// <summary>
	/// Gets a value indicating whether the point is inside the bounds of the text.
	/// </summary>
	public bool IsInside { get; }

	/// <summary>
	/// Gets the index of the hit character in the text.
	/// </summary>
	public int TextPosition { get; }

	/// <summary>
	/// Gets a value indicating whether the hit is on the trailing edge of the character.
	/// </summary>
	public bool IsTrailing { get; }

	public TextHitTestResult(CharacterHit characterHit, int textPosition, bool isInside, bool isTrailing)
	{
		CharacterHit = characterHit;
		TextPosition = textPosition;
		IsInside = isInside;
		IsTrailing = isTrailing;
	}
}
