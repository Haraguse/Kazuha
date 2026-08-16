using System;
using Avalonia.Data.Core;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.Parsers;

internal class PropertyParser
{
	public static (string? ns, string? owner, string name) Parse(string text)
	{
		return Parse(new CharacterReader(text.AsSpan()));
	}

	public static (string? ns, string? owner, string name) Parse(CharacterReader r)
	{
		if (r.End)
		{
			throw new ExpressionParseException(0, "Expected property name.");
		}
		bool flag = r.TakeIf('(');
		bool flag2 = false;
		string text = null;
		string text2 = null;
		string text3 = null;
		do
		{
			ReadOnlySpan<char> readOnlySpan = r.ParseIdentifier();
			if (readOnlySpan.IsEmpty)
			{
				if (r.End || (flag && !r.End && (flag2 = r.TakeIf(')'))))
				{
					break;
				}
				if (flag)
				{
					throw new ExpressionParseException(r.Position, "Expected ')'.");
				}
				throw new ExpressionParseException(r.Position, $"Unexpected '{r.Peek}'.");
			}
			if (!r.End && r.TakeIf(':'))
			{
				if (text != null)
				{
					throw new ExpressionParseException(r.Position, "Unexpected ':'.");
				}
				text = readOnlySpan.ToString();
			}
			else if (!r.End && r.TakeIf('.'))
			{
				if (text2 != null)
				{
					throw new ExpressionParseException(r.Position, "Unexpected '.'.");
				}
				text2 = readOnlySpan.ToString();
			}
			else
			{
				text3 = readOnlySpan.ToString();
			}
		}
		while (!r.End);
		if (text3 == null)
		{
			throw new ExpressionParseException(0, "Expected property name.");
		}
		if (flag && text2 == null)
		{
			throw new ExpressionParseException(1, "Expected property owner.");
		}
		if (flag && !flag2)
		{
			throw new ExpressionParseException(r.Position, "Expected ')'.");
		}
		if (!r.End)
		{
			throw new ExpressionParseException(r.Position, "Expected end of expression.");
		}
		return (ns: text, owner: text2, name: text3);
	}
}
