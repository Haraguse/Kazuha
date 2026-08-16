using System;

namespace Avalonia.Media.TextFormatting.Unicode;

public ref struct GraphemeEnumerator(ReadOnlySpan<char> text)
{
	private readonly ReadOnlySpan<char> _text = text;

	private int _currentCodeUnitOffset = 0;

	private int _codeUnitLengthOfCurrentCodepoint = 0;

	private Codepoint _currentCodepoint = Codepoint.ReplacementCodepoint;

	private IndicConjunctBreakClass _currentIndicConjunctBreakType = IndicConjunctBreakClass.None;

	/// <summary>
	/// Will be <see cref="F:Avalonia.Media.TextFormatting.Unicode.GraphemeBreakClass.Other" /> if invalid data or EOF reached.
	/// Caller shouldn't need to special-case this since the normal rules will halt on this condition.
	/// </summary>
	private GraphemeBreakClass _currentType = GraphemeBreakClass.Other;

	/// <summary>
	/// Moves to the next <see cref="T:Avalonia.Media.TextFormatting.Unicode.Grapheme" />.
	/// </summary>
	/// <returns></returns>
	public bool MoveNext(out Grapheme grapheme)
	{
		int currentCodeUnitOffset = _currentCodeUnitOffset;
		if ((uint)currentCodeUnitOffset >= (uint)_text.Length)
		{
			grapheme = default(Grapheme);
			return false;
		}
		if (currentCodeUnitOffset == 0)
		{
			ReadNextCodepoint();
		}
		Codepoint currentCodepoint = _currentCodepoint;
		if (_currentType == GraphemeBreakClass.Prepend)
		{
			do
			{
				ReadNextCodepoint();
			}
			while (_currentType == GraphemeBreakClass.Prepend);
			if ((uint)_currentCodeUnitOffset >= (uint)_text.Length)
			{
				goto IL_0250;
			}
		}
		bool hasIndicConjunctLinker;
		bool hasIndicConjunctBase;
		if (_currentCodeUnitOffset <= currentCodeUnitOffset || ((1 << (int)_currentType) & 0x206) == 0)
		{
			hasIndicConjunctLinker = false;
			hasIndicConjunctBase = false;
			ConsumeIndicConjunctBreak(_currentIndicConjunctBreakType);
			GraphemeBreakClass currentType = _currentType;
			ReadNextCodepoint();
			switch (currentType)
			{
			case GraphemeBreakClass.CR:
				if (_currentType == GraphemeBreakClass.LF)
				{
					ReadNextCodepoint();
				}
				break;
			case GraphemeBreakClass.L:
				while (_currentType == GraphemeBreakClass.L)
				{
					ReadNextCodepoint();
				}
				if (_currentType == GraphemeBreakClass.V)
				{
					ReadNextCodepoint();
				}
				else
				{
					if (_currentType != GraphemeBreakClass.LV)
					{
						if (_currentType == GraphemeBreakClass.LVT)
						{
							ReadNextCodepoint();
							goto case GraphemeBreakClass.LVT;
						}
						goto default;
					}
					ReadNextCodepoint();
				}
				goto case GraphemeBreakClass.LV;
			case GraphemeBreakClass.LV:
			case GraphemeBreakClass.V:
				while (_currentType == GraphemeBreakClass.V)
				{
					ReadNextCodepoint();
				}
				if (_currentType == GraphemeBreakClass.T)
				{
					ReadNextCodepoint();
					goto case GraphemeBreakClass.LVT;
				}
				goto default;
			case GraphemeBreakClass.LVT:
			case GraphemeBreakClass.T:
				while (_currentType == GraphemeBreakClass.T)
				{
					ReadNextCodepoint();
				}
				goto default;
			case GraphemeBreakClass.ExtendedPictographic:
				while (true)
				{
					if (_currentType == GraphemeBreakClass.Extend)
					{
						ReadNextCodepoint();
						continue;
					}
					if (_currentType != GraphemeBreakClass.ZWJ)
					{
						break;
					}
					ReadNextCodepoint();
					if (_currentType != GraphemeBreakClass.ExtendedPictographic)
					{
						break;
					}
					ReadNextCodepoint();
				}
				goto default;
			case GraphemeBreakClass.RegionalIndicator:
				if (_currentType == GraphemeBreakClass.RegionalIndicator)
				{
					ReadNextCodepoint();
				}
				goto default;
			default:
				while (((1 << (int)_currentType) & 0x24040) != 0)
				{
					ConsumeIndicConjunctBreak(_currentIndicConjunctBreakType);
					ReadNextCodepoint();
				}
				while ((hasIndicConjunctBase & hasIndicConjunctLinker) && _currentIndicConjunctBreakType == IndicConjunctBreakClass.Consonant)
				{
					ConsumeIndicConjunctBreak(_currentIndicConjunctBreakType);
					ReadNextCodepoint();
					while (((1 << (int)_currentType) & 0x24040) != 0)
					{
						ConsumeIndicConjunctBreak(_currentIndicConjunctBreakType);
						ReadNextCodepoint();
					}
				}
				break;
			case GraphemeBreakClass.Control:
			case GraphemeBreakClass.LF:
				break;
			}
		}
		goto IL_0250;
		IL_0250:
		int length = _currentCodeUnitOffset - currentCodeUnitOffset;
		grapheme = new Grapheme(currentCodepoint, currentCodeUnitOffset, length);
		return true;
		void ConsumeIndicConjunctBreak(IndicConjunctBreakClass indicConjunctBreakType)
		{
			switch (indicConjunctBreakType)
			{
			case IndicConjunctBreakClass.Consonant:
				hasIndicConjunctBase = true;
				hasIndicConjunctLinker = false;
				break;
			case IndicConjunctBreakClass.Linker:
				if (hasIndicConjunctBase)
				{
					hasIndicConjunctLinker = true;
				}
				break;
			default:
				hasIndicConjunctBase = false;
				hasIndicConjunctLinker = false;
				break;
			case IndicConjunctBreakClass.Extend:
				break;
			}
		}
	}

	private void ReadNextCodepoint()
	{
		_currentCodeUnitOffset += _codeUnitLengthOfCurrentCodepoint;
		_currentCodepoint = Codepoint.ReadAt(_text, _currentCodeUnitOffset, out _codeUnitLengthOfCurrentCodepoint);
		_currentType = _currentCodepoint.GraphemeBreakClass;
		_currentIndicConjunctBreakType = UnicodeData.GetIndicConjunctBreakClass(_currentCodepoint.Value);
	}
}
