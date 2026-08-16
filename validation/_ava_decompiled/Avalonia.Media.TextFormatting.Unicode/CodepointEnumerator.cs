using System;

namespace Avalonia.Media.TextFormatting.Unicode;

public ref struct CodepointEnumerator(ReadOnlySpan<char> text)
{
	private readonly ReadOnlySpan<char> _text = text;

	private int _offset = 0;

	/// <summary>
	/// Moves to the next <see cref="T:Avalonia.Media.TextFormatting.Unicode.Codepoint" />.
	/// </summary>
	/// <returns></returns>
	public bool MoveNext(out Codepoint codepoint)
	{
		if ((uint)_offset >= (uint)_text.Length)
		{
			codepoint = Codepoint.ReplacementCodepoint;
			return false;
		}
		codepoint = Codepoint.ReadAt(_text, _offset, out var count);
		_offset += count;
		return true;
	}
}
