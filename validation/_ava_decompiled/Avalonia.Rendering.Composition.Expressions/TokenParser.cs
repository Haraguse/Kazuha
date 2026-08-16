using System;
using System.Globalization;

namespace Avalonia.Rendering.Composition.Expressions;

/// <summary>
/// Helper class for composition expression parser
/// </summary>
internal ref struct TokenParser
{
	private ReadOnlySpan<char> _s;

	public int Position { get; private set; }

	public int Length => _s.Length;

	public TokenParser(ReadOnlySpan<char> s)
	{
		_s = s;
		Position = 0;
	}

	public void SkipWhitespace()
	{
		while (_s.Length > 0 && char.IsWhiteSpace(_s[0]))
		{
			Advance(1);
		}
	}

	public bool NextIsWhitespace()
	{
		if (_s.Length > 0)
		{
			return char.IsWhiteSpace(_s[0]);
		}
		return false;
	}

	private static bool IsAlphaNumeric(char ch)
	{
		if ((ch < '0' || ch > '9') && (ch < 'a' || ch > 'z'))
		{
			if (ch >= 'A')
			{
				return ch <= 'Z';
			}
			return false;
		}
		return true;
	}

	public bool TryConsume(char c)
	{
		SkipWhitespace();
		if (_s.Length == 0 || _s[0] != c)
		{
			return false;
		}
		Advance(1);
		return true;
	}

	public bool TryConsume(string s)
	{
		SkipWhitespace();
		if (_s.Length < s.Length)
		{
			return false;
		}
		for (int i = 0; i < s.Length; i++)
		{
			if (_s[i] != s[i])
			{
				return false;
			}
		}
		Advance(s.Length);
		return true;
	}

	public bool TryConsumeAny(ReadOnlySpan<char> chars, out char token)
	{
		SkipWhitespace();
		token = '\0';
		if (_s.Length == 0)
		{
			return false;
		}
		ReadOnlySpan<char> readOnlySpan = chars;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			if (c == _s[0])
			{
				token = c;
				Advance(1);
				return true;
			}
		}
		return false;
	}

	public bool TryParseKeyword(string keyword)
	{
		SkipWhitespace();
		if (keyword.Length > _s.Length)
		{
			return false;
		}
		for (int i = 0; i < keyword.Length; i++)
		{
			if (keyword[i] != _s[i])
			{
				return false;
			}
		}
		if (_s.Length > keyword.Length && IsAlphaNumeric(_s[keyword.Length]))
		{
			return false;
		}
		Advance(keyword.Length);
		return true;
	}

	public bool TryParseKeywordLowerCase(string keywordInLowerCase)
	{
		SkipWhitespace();
		if (keywordInLowerCase.Length > _s.Length)
		{
			return false;
		}
		for (int i = 0; i < keywordInLowerCase.Length; i++)
		{
			if (keywordInLowerCase[i] != char.ToLowerInvariant(_s[i]))
			{
				return false;
			}
		}
		if (_s.Length > keywordInLowerCase.Length && IsAlphaNumeric(_s[keywordInLowerCase.Length]))
		{
			return false;
		}
		Advance(keywordInLowerCase.Length);
		return true;
	}

	public void Advance(int c)
	{
		_s = _s.Slice(c);
		Position += c;
	}

	public bool TryParseIdentifier(ReadOnlySpan<char> extraValidChars, out ReadOnlySpan<char> res)
	{
		res = ReadOnlySpan<char>.Empty;
		SkipWhitespace();
		if (_s.Length == 0)
		{
			return false;
		}
		char c = _s[0];
		if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z'))
		{
			return false;
		}
		int num = 1;
		for (int i = 1; i < _s.Length; i++)
		{
			char c2 = _s[i];
			if (IsAlphaNumeric(c2))
			{
				num++;
				continue;
			}
			bool flag = false;
			ReadOnlySpan<char> readOnlySpan = extraValidChars;
			for (int j = 0; j < readOnlySpan.Length; j++)
			{
				if (readOnlySpan[j] == c2)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
			num++;
		}
		res = _s.Slice(0, num);
		Advance(num);
		return true;
	}

	public bool TryParseIdentifier(out ReadOnlySpan<char> res)
	{
		res = ReadOnlySpan<char>.Empty;
		SkipWhitespace();
		if (_s.Length == 0)
		{
			return false;
		}
		char c = _s[0];
		if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z'))
		{
			return false;
		}
		int num = 1;
		for (int i = 1; i < _s.Length && IsAlphaNumeric(_s[i]); i++)
		{
			num++;
		}
		res = _s.Slice(0, num);
		Advance(num);
		return true;
	}

	public bool TryParseCall(out ReadOnlySpan<char> res)
	{
		res = ReadOnlySpan<char>.Empty;
		SkipWhitespace();
		if (_s.Length == 0)
		{
			return false;
		}
		char c = _s[0];
		if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z'))
		{
			return false;
		}
		int num = 1;
		for (int i = 1; i < _s.Length; i++)
		{
			char c2 = _s[i];
			if ((c2 < '0' || c2 > '9') && (c2 < 'a' || c2 > 'z') && (c2 < 'A' || c2 > 'Z') && c2 != '.')
			{
				break;
			}
			num++;
		}
		res = _s.Slice(0, num);
		for (int j = num; j < _s.Length; j++)
		{
			if (!char.IsWhiteSpace(_s[j]))
			{
				if (_s[j] == '(')
				{
					Advance(j + 1);
					return true;
				}
				return false;
			}
		}
		return false;
	}

	public bool TryParseFloat(out float res)
	{
		res = 0f;
		SkipWhitespace();
		if (_s.Length == 0)
		{
			return false;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < _s.Length; i++)
		{
			char c = _s[i];
			if (c >= '0' && c <= '9')
			{
				num = i + 1;
				continue;
			}
			if (c == '.' && num2 == 0)
			{
				num = i + 1;
				num2++;
				continue;
			}
			if (c != '-' || num != 0)
			{
				break;
			}
			num = i + 1;
		}
		if (!float.TryParse(_s.Slice(0, num), NumberStyles.Number, CultureInfo.InvariantCulture, out res))
		{
			return false;
		}
		Advance(num);
		return true;
	}

	public bool TryParseDouble(out double res)
	{
		res = 0.0;
		SkipWhitespace();
		if (_s.Length == 0)
		{
			return false;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < _s.Length; i++)
		{
			char c = _s[i];
			if (c >= '0' && c <= '9')
			{
				num = i + 1;
				continue;
			}
			if (c == '.' && num2 == 0)
			{
				num = i + 1;
				num2++;
				continue;
			}
			if (c != '-')
			{
				break;
			}
			if (num != 0)
			{
				return false;
			}
			num = i + 1;
		}
		if (!double.TryParse(_s.Slice(0, num), NumberStyles.Number, CultureInfo.InvariantCulture, out res))
		{
			return false;
		}
		Advance(num);
		return true;
	}

	public bool IsEofWithWhitespace()
	{
		SkipWhitespace();
		return Length == 0;
	}

	public override string ToString()
	{
		return _s.ToString();
	}
}
