using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// A collection that holds <see cref="T:Avalonia.Media.TextDecoration" /> objects.
/// </summary>
public class TextDecorationCollection : AvaloniaList<TextDecoration>
{
	public TextDecorationCollection()
	{
	}

	public TextDecorationCollection(IEnumerable<TextDecoration> textDecorations)
		: base(textDecorations)
	{
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Media.TextDecorationCollection" /> string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The <see cref="T:Avalonia.Media.TextDecorationCollection" />.</returns>
	public static TextDecorationCollection Parse(string s)
	{
		List<TextDecorationLocation> list = new List<TextDecorationLocation>();
		using (SpanStringTokenizer spanStringTokenizer = new SpanStringTokenizer(s, ',', "Invalid text decoration."))
		{
			ReadOnlySpan<char> result;
			while (spanStringTokenizer.TryReadSpan(out result))
			{
				TextDecorationLocation textDecorationLocation = GetTextDecorationLocation(result);
				if (list.Contains(textDecorationLocation))
				{
					throw new ArgumentException("Text decoration already specified.", "s");
				}
				list.Add(textDecorationLocation);
			}
		}
		TextDecorationCollection textDecorationCollection = new TextDecorationCollection();
		foreach (TextDecorationLocation item in list)
		{
			textDecorationCollection.Add(new TextDecoration
			{
				Location = item
			});
		}
		return textDecorationCollection;
	}

	/// <summary>
	/// Parses a <see cref="T:Avalonia.Media.TextDecorationLocation" /> string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>The <see cref="T:Avalonia.Media.TextDecorationLocation" />.</returns>
	private static TextDecorationLocation GetTextDecorationLocation(ReadOnlySpan<char> s)
	{
		if (s.TryParseEnum<TextDecorationLocation>(ignoreCase: true, out var value))
		{
			return value;
		}
		throw new ArgumentException("Could not parse text decoration.", "s");
	}
}
