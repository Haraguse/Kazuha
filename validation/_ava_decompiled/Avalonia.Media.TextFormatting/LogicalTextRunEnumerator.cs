using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Walks the runs of a <see cref="T:Avalonia.Media.TextFormatting.TextLine" /> in <b>logical</b> (source-text)
/// order. This is the order that splits and length-based offsets are defined
/// in, and is what every <see cref="M:Avalonia.Media.TextFormatting.TextCollapsingProperties.Collapse(Avalonia.Media.TextFormatting.TextLine)" />
/// implementation needs to see — unlike <see cref="P:Avalonia.Media.TextFormatting.TextLine.TextRuns" />,
/// which exposes the post-BiDi <i>visual</i> ordering used for rendering.
/// </summary>
/// <remarks>
/// When the line has been finalized (the normal case after
/// <c>TextLineImpl.FinalizeLine</c>), the enumerator iterates over
/// <c>_indexedTextRuns</c> — a level-resolved table that maps each run back
/// to its original logical position. If the line hasn't been finalized (or
/// the line is not a <c>TextLineImpl</c>), it falls back to the raw
/// <see cref="P:Avalonia.Media.TextFormatting.TextLine.TextRuns" /> list.
/// </remarks>
internal ref struct LogicalTextRunEnumerator
{
	private readonly IReadOnlyList<TextRun>? _textRuns = null;

	private readonly IReadOnlyList<IndexedTextRun>? _indexedTextRuns = null;

	private readonly int _step;

	private readonly int _end;

	private int _index;

	public int Count { get; } = 0;

	public LogicalTextRunEnumerator(TextLine line, bool backward = false)
	{
		IReadOnlyList<IndexedTextRun> readOnlyList = (line as TextLineImpl)?._indexedTextRuns;
		if (readOnlyList != null && readOnlyList.Count > 0)
		{
			_indexedTextRuns = readOnlyList;
			Count = readOnlyList.Count;
		}
		else if (line.TextRuns.Count > 0)
		{
			_textRuns = line.TextRuns;
			Count = _textRuns.Count;
		}
		if (backward)
		{
			_step = -1;
			_end = -1;
			_index = Count;
		}
		else
		{
			_step = 1;
			_end = Count;
			_index = -1;
		}
	}

	public bool MoveNext([MaybeNullWhen(false)] out TextRun run)
	{
		_index += _step;
		if (_index == _end)
		{
			run = null;
			return false;
		}
		if (_indexedTextRuns != null)
		{
			run = _indexedTextRuns[_index].TextRun;
		}
		else
		{
			if (_textRuns == null)
			{
				run = null;
				return false;
			}
			run = _textRuns[_index];
		}
		return true;
	}
}
