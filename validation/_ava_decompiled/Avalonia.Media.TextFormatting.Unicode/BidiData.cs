using System;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting.Unicode;

/// <summary>
/// Represents a unicode string and all associated attributes
/// for each character required for the bidirectional Unicode algorithm
/// </summary>
/// <remarks>To avoid allocations, this class is designed to be reused.</remarks>
internal sealed class BidiData
{
	private bool _hasCleanState = true;

	private ArrayBuilder<BidiClass> _classes;

	private ArrayBuilder<BidiPairedBracketType> _pairedBracketTypes;

	private ArrayBuilder<int> _pairedBracketValues;

	private ArrayBuilder<BidiClass> _savedClasses;

	private ArrayBuilder<BidiPairedBracketType> _savedPairedBracketTypes;

	private ArrayBuilder<sbyte> _tempLevelBuffer;

	public sbyte ParagraphEmbeddingLevel { get; set; }

	public bool? HasBrackets { get; private set; }

	public bool? HasEmbeddings { get; private set; }

	public bool? HasIsolates { get; private set; }

	/// <summary>
	/// Gets the length of the data held by the BidiData
	/// </summary>
	public int Length { get; private set; }

	/// <summary>
	/// Gets the bidi character type of each code point
	/// </summary>
	public ArraySlice<BidiClass> Classes { get; private set; }

	/// <summary>
	/// Gets the paired bracket type for each code point
	/// </summary>
	public ArraySlice<BidiPairedBracketType> PairedBracketTypes { get; private set; }

	/// <summary>
	/// Gets the paired bracket value for code point
	/// </summary>
	/// <remarks>
	/// The paired bracket values are the code points
	/// of each character where the opening code point
	/// is replaced with the closing code point for easier
	/// matching.  Also, bracket code points are mapped
	/// to their canonical equivalents
	/// </remarks>
	public ArraySlice<int> PairedBracketValues { get; private set; }

	/// <summary>
	/// Appends text to the bidi data.
	/// </summary>
	/// <param name="text">The text to process.</param>
	public void Append(ReadOnlySpan<char> text)
	{
		_hasCleanState = false;
		_classes.Add(text.Length);
		_pairedBracketTypes.Add(text.Length);
		_pairedBracketValues.Add(text.Length);
		int num = Length;
		CodepointEnumerator codepointEnumerator = new CodepointEnumerator(text);
		Codepoint codepoint;
		while (codepointEnumerator.MoveNext(out codepoint))
		{
			BidiClass biDiClass = codepoint.BiDiClass;
			_classes[num] = biDiClass;
			uint num2 = (uint)(1 << (int)biDiClass);
			if (!HasEmbeddings.HasValue && (num2 & 0x149400) != 0)
			{
				HasEmbeddings = true;
			}
			if (!HasIsolates.HasValue && (num2 & 0x90A00) != 0)
			{
				HasIsolates = true;
			}
			BidiPairedBracketType pairedBracketType = codepoint.PairedBracketType;
			_pairedBracketTypes[num] = pairedBracketType;
			switch (pairedBracketType)
			{
			case BidiPairedBracketType.Open:
			{
				codepoint.TryGetPairedBracket(out var codepoint2);
				_pairedBracketValues[num] = (int)Codepoint.GetCanonicalType(codepoint2).Value;
				HasBrackets = true;
				break;
			}
			case BidiPairedBracketType.Close:
				_pairedBracketValues[num] = (int)Codepoint.GetCanonicalType(codepoint).Value;
				HasBrackets = true;
				break;
			}
			num++;
		}
		Length = num;
		Classes = _classes.AsSlice(0, Length);
		PairedBracketTypes = _pairedBracketTypes.AsSlice(0, Length);
		PairedBracketValues = _pairedBracketValues.AsSlice(0, Length);
	}

	/// <summary>
	/// Save the Types and PairedBracketTypes of this BiDiData
	/// </summary>
	/// <remarks>
	/// This is used when processing embedded style runs with
	/// BiDiClass overrides. Text layout process saves the data,
	/// overrides the style runs to neutral, processes the bidi
	/// data for the entire paragraph and then restores this data
	/// before processing the embedded runs.
	/// </remarks>
	public void SaveTypes()
	{
		_hasCleanState = false;
		_savedClasses.Clear();
		_savedClasses.Add(_classes.AsSlice());
		_savedPairedBracketTypes.Clear();
		_savedPairedBracketTypes.Add(_pairedBracketTypes.AsSlice());
	}

	/// <summary>
	/// Restore the data saved by SaveTypes
	/// </summary>
	public void RestoreTypes()
	{
		_hasCleanState = false;
		_classes.Clear();
		_classes.Add(_savedClasses.AsSlice());
		_pairedBracketTypes.Clear();
		_pairedBracketTypes.Add(_savedPairedBracketTypes.AsSlice());
	}

	/// <summary>
	/// Gets a temporary level buffer. Used by the text layout process when
	/// resolving style runs with different BiDiClass.
	/// </summary>
	/// <param name="length">Length of the required ExpandableBuffer</param>
	/// <returns>An uninitialized level ExpandableBuffer</returns>
	public ArraySlice<sbyte> GetTempLevelBuffer(int length)
	{
		_tempLevelBuffer.Clear();
		return _tempLevelBuffer.Add(length, clear: false);
	}

	/// <summary>
	/// Resets the bidi data to a clean state.
	/// </summary>
	public void Reset()
	{
		if (!_hasCleanState)
		{
			FormattingBufferHelper.ClearThenResetIfTooLarge(ref _classes);
			FormattingBufferHelper.ClearThenResetIfTooLarge(ref _pairedBracketTypes);
			FormattingBufferHelper.ClearThenResetIfTooLarge(ref _pairedBracketValues);
			FormattingBufferHelper.ClearThenResetIfTooLarge(ref _savedClasses);
			FormattingBufferHelper.ClearThenResetIfTooLarge(ref _savedPairedBracketTypes);
			FormattingBufferHelper.ClearThenResetIfTooLarge(ref _tempLevelBuffer);
			ParagraphEmbeddingLevel = 0;
			HasBrackets = false;
			HasEmbeddings = false;
			HasIsolates = false;
			Length = 0;
			Classes = default(ArraySlice<BidiClass>);
			PairedBracketTypes = default(ArraySlice<BidiPairedBracketType>);
			PairedBracketValues = default(ArraySlice<int>);
			_hasCleanState = true;
		}
	}
}
