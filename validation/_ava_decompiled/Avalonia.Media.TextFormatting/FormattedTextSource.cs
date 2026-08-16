using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting;

internal readonly struct FormattedTextSource(string text, TextRunProperties defaultProperties, IReadOnlyList<ValueSpan<TextRunProperties>>? textModifier) : ITextSource
{
	/// <summary>
	/// References a portion of a text buffer.
	/// </summary>
	private readonly record struct TextRange
	{
		/// <summary>
		/// Gets the start.
		/// </summary>
		/// <value>
		/// The start.
		/// </value>
		public int Start { get; }

		/// <summary>
		/// Gets the length.
		/// </summary>
		/// <value>
		/// The length.
		/// </value>
		public int Length { get; }

		/// <summary>
		/// Gets the end.
		/// </summary>
		/// <value>
		/// The end.
		/// </value>
		public int End => Start + Length - 1;

		public TextRange(int start, int length)
		{
			Start = start;
			Length = length;
		}

		/// <summary>
		/// Returns a specified number of contiguous elements from the start of the slice.
		/// </summary>
		/// <param name="length">The number of elements to return.</param>
		/// <returns>A <see cref="T:Avalonia.Media.TextFormatting.FormattedTextSource.TextRange" /> that contains the specified number of elements from the start of this slice.</returns>
		public TextRange Take(int length)
		{
			if (length > Length)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			return new TextRange(Start, length);
		}

		/// <summary>
		/// Bypasses a specified number of elements in the slice and then returns the remaining elements.
		/// </summary>
		/// <param name="length">The number of elements to skip before returning the remaining elements.</param>
		/// <returns>A <see cref="T:Avalonia.Media.TextFormatting.FormattedTextSource.TextRange" /> that contains the elements that occur after the specified index in this slice.</returns>
		public TextRange Skip(int length)
		{
			if (length > Length)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			return new TextRange(Start + length, Length - length);
		}
	}

	private readonly string _text = text;

	private readonly TextRunProperties _defaultProperties = defaultProperties;

	private readonly IReadOnlyList<ValueSpan<TextRunProperties>>? _textModifier = textModifier;

	public TextRun? GetTextRun(int textSourceIndex)
	{
		if (textSourceIndex > _text.Length)
		{
			return null;
		}
		ReadOnlySpan<char> text = _text.AsSpan(textSourceIndex);
		if (text.IsEmpty)
		{
			return null;
		}
		ValueSpan<TextRunProperties> valueSpan = CreateTextStyleRun(text, textSourceIndex, _defaultProperties, _textModifier);
		return new TextCharacters(_text.AsMemory(textSourceIndex, valueSpan.Length), valueSpan.Value);
	}

	/// <summary>
	/// Creates a span of text run properties that has modifier applied.
	/// </summary>
	/// <param name="text">The text to create the properties for.</param>
	/// <param name="firstTextSourceIndex">The first text source index.</param>
	/// <param name="defaultProperties">The default text properties.</param>
	/// <param name="textModifier">The text properties modifier.</param>
	/// <returns>
	/// The created text style run.
	/// </returns>
	internal static ValueSpan<TextRunProperties> CreateTextStyleRun(ReadOnlySpan<char> text, int firstTextSourceIndex, TextRunProperties defaultProperties, IReadOnlyList<ValueSpan<TextRunProperties>>? textModifier)
	{
		if (textModifier == null || textModifier.Count == 0)
		{
			return new ValueSpan<TextRunProperties>(firstTextSourceIndex, text.Length, defaultProperties);
		}
		TextRunProperties textRunProperties = defaultProperties;
		int i = 0;
		int num = 0;
		for (; i < textModifier.Count; i++)
		{
			ValueSpan<TextRunProperties> valueSpan = textModifier[i];
			TextRange textRange = new TextRange(valueSpan.Start, valueSpan.Length);
			if (textRange.Start + textRange.Length > firstTextSourceIndex)
			{
				if (textRange.Start > firstTextSourceIndex + text.Length)
				{
					num = text.Length;
					break;
				}
				if (textRange.Start > firstTextSourceIndex && valueSpan.Value != textRunProperties)
				{
					num = Math.Min(Math.Abs(textRange.Start - firstTextSourceIndex), text.Length);
					break;
				}
				num = Math.Max(0, textRange.Start + textRange.Length - firstTextSourceIndex);
				textRunProperties = valueSpan.Value;
				break;
			}
		}
		if (num < text.Length && i == textModifier.Count && textRunProperties == defaultProperties)
		{
			num = text.Length;
		}
		if (num == 0 && textRunProperties != defaultProperties)
		{
			textRunProperties = defaultProperties;
			num = text.Length;
		}
		num = CoerceLength(text, num);
		return new ValueSpan<TextRunProperties>(firstTextSourceIndex, num, textRunProperties);
	}

	private static int CoerceLength(ReadOnlySpan<char> text, int length)
	{
		int num = 0;
		GraphemeEnumerator graphemeEnumerator = new GraphemeEnumerator(text);
		Grapheme grapheme;
		while (graphemeEnumerator.MoveNext(out grapheme))
		{
			num += grapheme.Length;
			if (num >= length)
			{
				return num;
			}
		}
		return Math.Min(length, text.Length);
	}
}
