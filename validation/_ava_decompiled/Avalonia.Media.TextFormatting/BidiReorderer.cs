using System;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Reorders text runs according to their bidi level.
/// </summary>
/// <remarks>To avoid allocations, this class is designed to be reused.</remarks>
internal sealed class BidiReorderer
{
	private struct BidiRange(sbyte level, int leftRunIndex, int rightRunIndex, int previousRangeIndex)
	{
		public sbyte Level { get; set; } = level;

		public int LeftRunIndex { get; set; } = leftRunIndex;

		public int RightRunIndex { get; set; } = rightRunIndex;

		public int PreviousRangeIndex { get; } = previousRangeIndex;
	}

	[ThreadStatic]
	private static BidiReorderer? t_instance;

	private ArrayBuilder<OrderedBidiRun> _runs;

	private ArrayBuilder<BidiRange> _ranges;

	public static BidiReorderer Instance => t_instance ?? (t_instance = new BidiReorderer());

	public IndexedTextRun[] BidiReorder(Span<TextRun> textRuns, FlowDirection flowDirection, int firstTextSourceIndex)
	{
		if (textRuns.IsEmpty)
		{
			return Array.Empty<IndexedTextRun>();
		}
		try
		{
			sbyte? previousLevel = null;
			_runs.Add(textRuns.Length);
			for (int i = 0; i < textRuns.Length; i++)
			{
				TextRun run = textRuns[i];
				OrderedBidiRun orderedBidiRun = new OrderedBidiRun(i, run, GetRunBidiLevel(run, flowDirection, previousLevel));
				_runs[i] = orderedBidiRun;
				if (i > 0)
				{
					_runs[i - 1].NextRunIndex = i;
				}
				previousLevel = orderedBidiRun.Level;
			}
			int num = LinearReorder();
			IndexedTextRun[] array = new IndexedTextRun[textRuns.Length];
			for (int j = 0; j < textRuns.Length; j++)
			{
				TextRun textRun = textRuns[j];
				array[j] = new IndexedTextRun
				{
					TextRun = textRun,
					TextSourceCharacterIndex = firstTextSourceIndex,
					RunIndex = j,
					NextRunIndex = j + 1
				};
				firstTextSourceIndex += textRun.Length;
			}
			int num2 = 0;
			int num3 = num;
			while (num3 >= 0)
			{
				ref OrderedBidiRun reference = ref _runs[num3];
				textRuns[num2] = reference.Run;
				IndexedTextRun obj = array[num2];
				obj.RunIndex = reference.RunIndex;
				obj.NextRunIndex = reference.NextRunIndex;
				num2++;
				num3 = reference.NextRunIndex;
			}
			return array;
		}
		finally
		{
			FormattingBufferHelper.ClearThenResetIfTooLarge(ref _runs);
			FormattingBufferHelper.ClearThenResetIfTooLarge(ref _ranges);
		}
	}

	private static sbyte GetRunBidiLevel(TextRun run, FlowDirection flowDirection, sbyte? previousLevel)
	{
		if (run is ShapedTextRun shapedTextRun)
		{
			return shapedTextRun.BidiLevel;
		}
		sbyte result = ((flowDirection != FlowDirection.LeftToRight) ? ((sbyte)1) : ((sbyte)0));
		if (run is TextEndOfLine)
		{
			return 0;
		}
		if (previousLevel.HasValue)
		{
			return previousLevel.Value;
		}
		return result;
	}

	/// <summary>
	/// Reorders the runs from logical to visual order.
	/// <see href="https://github.com/fribidi/linear-reorder/blob/f2f872257d4d8b8e137fcf831f254d6d4db79d3c/linear-reorder.c" />
	/// </summary>
	/// <returns>The first run index in visual order.</returns>
	private int LinearReorder()
	{
		int num = 0;
		int num2 = -1;
		while (num >= 0)
		{
			ref OrderedBidiRun reference = ref _runs[num];
			int nextRunIndex = reference.NextRunIndex;
			while (num2 >= 0 && _ranges[num2].Level > reference.Level && _ranges[num2].PreviousRangeIndex >= 0 && _ranges[_ranges[num2].PreviousRangeIndex].Level >= reference.Level)
			{
				num2 = MergeRangeWithPrevious(num2);
			}
			if (num2 >= 0 && _ranges[num2].Level >= reference.Level)
			{
				if ((reference.Level & 1) != 0)
				{
					reference.NextRunIndex = _ranges[num2].LeftRunIndex;
					_ranges[num2].LeftRunIndex = num;
				}
				else
				{
					_runs[_ranges[num2].RightRunIndex].NextRunIndex = num;
					_ranges[num2].RightRunIndex = num;
				}
				_ranges[num2].Level = reference.Level;
			}
			else
			{
				BidiRange value = new BidiRange(reference.Level, num, num, num2);
				_ranges.AddItem(value);
				num2 = _ranges.Length - 1;
			}
			num = nextRunIndex;
		}
		while (num2 >= 0 && _ranges[num2].PreviousRangeIndex >= 0)
		{
			num2 = MergeRangeWithPrevious(num2);
		}
		_runs[_ranges[num2].RightRunIndex].NextRunIndex = -1;
		return _runs[_ranges[num2].LeftRunIndex].RunIndex;
	}

	private int MergeRangeWithPrevious(int index)
	{
		int previousRangeIndex = _ranges[index].PreviousRangeIndex;
		ref BidiRange reference = ref _ranges[previousRangeIndex];
		int index2;
		int index3;
		if ((reference.Level & 1) != 0)
		{
			index2 = index;
			index3 = previousRangeIndex;
		}
		else
		{
			index2 = previousRangeIndex;
			index3 = index;
		}
		ref BidiRange reference2 = ref _ranges[index2];
		ref BidiRange reference3 = ref _ranges[index3];
		_runs[reference2.RightRunIndex].NextRunIndex = _runs[reference3.LeftRunIndex].RunIndex;
		reference.LeftRunIndex = reference2.LeftRunIndex;
		reference.RightRunIndex = reference3.RightRunIndex;
		return previousRangeIndex;
	}
}
