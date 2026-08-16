using System;

namespace Avalonia.Utilities;

internal ref struct CharacterReader
{
	private ReadOnlySpan<char> _s;

	public bool End => _s.IsEmpty;

	public char Peek => _s[0];

	public int Position { get; private set; }

	public CharacterReader(ReadOnlySpan<char> s)
	{
		this = default(CharacterReader);
		_s = s;
	}

	public char Take()
	{
		Position++;
		char result = _s[0];
		_s = _s.Slice(1);
		return result;
	}

	public void SkipWhitespace()
	{
		ReadOnlySpan<char> s = _s.TrimStart();
		Position += _s.Length - s.Length;
		_s = s;
	}

	public bool TakeIf(char c)
	{
		if (Peek == c)
		{
			Take();
			return true;
		}
		return false;
	}

	internal bool TakeIf(string s)
	{
		if (TryPeek(s.Length).SequenceEqual(s.AsSpan()))
		{
			_s = _s.Slice(s.Length);
			Position += s.Length;
			return true;
		}
		return false;
	}

	public bool TakeIf(Func<char, bool> condition)
	{
		if (condition(Peek))
		{
			Take();
			return true;
		}
		return false;
	}

	public ReadOnlySpan<char> TakeUntil(char c)
	{
		int i;
		for (i = 0; i < _s.Length && _s[i] != c; i++)
		{
		}
		ReadOnlySpan<char> result = _s.Slice(0, i);
		_s = _s.Slice(i);
		Position += i;
		return result;
	}

	public ReadOnlySpan<char> TakeWhile(Func<char, bool> condition)
	{
		int i;
		for (i = 0; i < _s.Length && condition(_s[i]); i++)
		{
		}
		ReadOnlySpan<char> result = _s.Slice(0, i);
		_s = _s.Slice(i);
		Position += i;
		return result;
	}

	public ReadOnlySpan<char> TryPeek(int count)
	{
		if (_s.Length < count)
		{
			return ReadOnlySpan<char>.Empty;
		}
		return _s.Slice(0, count);
	}

	public ReadOnlySpan<char> PeekWhitespace()
	{
		ReadOnlySpan<char> readOnlySpan = _s.TrimStart();
		return _s.Slice(0, _s.Length - readOnlySpan.Length);
	}

	public void Skip(int count)
	{
		if (_s.Length < count)
		{
			throw new IndexOutOfRangeException();
		}
		_s = _s.Slice(count);
	}
}
