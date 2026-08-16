using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Provides text collapsing properties that replace the middle segments of a file path with an ellipsis symbol when
/// the rendered width exceeds a specified limit.
/// </summary>
/// <remarks>This class is typically used to display file paths in a compact form by collapsing segments
/// near the center and inserting an ellipsis, ensuring that the most significant parts of the path remain visible.
/// </remarks>
public sealed class TextPathSegmentEllipsis : TextCollapsingProperties
{
	private readonly char[] _separators = new char[4]
	{
		Path.DirectorySeparatorChar,
		Path.AltDirectorySeparatorChar,
		'/',
		'\\'
	};

	public override double Width { get; }

	public override TextRun Symbol { get; }

	public override FlowDirection FlowDirection { get; }

	/// <summary>
	/// Initializes a new instance of the TextPathSegmentEllipsis class that represents an ellipsis segment in a
	/// text path with the specified symbol, width, text formatting properties, and flow direction.
	/// </summary>
	/// <param name="ellipsis">The string to use as the ellipsis symbol in the text path segment. Cannot be null.</param>
	/// <param name="width">The width.</param>
	/// <param name="textRunProperties">The text formatting properties to apply to the ellipsis symbol. Cannot be null.</param>
	/// <param name="flowDirection">The flow direction for rendering the ellipsis segment. Specifies whether text flows left-to-right or
	/// right-to-left.</param>
	public TextPathSegmentEllipsis(string ellipsis, double width, TextRunProperties textRunProperties, FlowDirection flowDirection)
	{
		Width = width;
		Symbol = new TextCharacters(ellipsis, textRunProperties);
		FlowDirection = flowDirection;
	}

	public override TextRun[]? Collapse(TextLine textLine)
	{
		if (textLine.TextRuns.Count == 0)
		{
			return null;
		}
		FormattingObjectPool instance = FormattingObjectPool.Instance;
		ShapedTextRun shapedTextRun = TextFormatter.CreateSymbol(Symbol, FlowDirection);
		if (MathUtilities.LessThan(Width, shapedTextRun.Size.Width))
		{
			return null;
		}
		double width = textLine.Width;
		if (MathUtilities.LessThanOrClose(width, Width))
		{
			return null;
		}
		FormattingObjectPool.RentedList<TextRun> rentedList = null;
		try
		{
			rentedList = instance.TextRunLists.Rent();
			LogicalTextRunEnumerator logicalTextRunEnumerator = new LogicalTextRunEnumerator(textLine);
			TextRun run;
			while (logicalTextRunEnumerator.MoveNext(out run))
			{
				rentedList.Add(run);
			}
			int[] array = new int[rentedList.Count + 1];
			for (int i = 0; i < rentedList.Count; i++)
			{
				array[i + 1] = array[i] + rentedList[i].Length;
			}
			List<(int, int, double, bool)> list = new List<(int, int, double, bool)>();
			List<int> list2 = new List<int>();
			int num = 0;
			int num2 = 0;
			bool flag = false;
			for (int j = 0; j < rentedList.Count; j++)
			{
				TextRun textRun = rentedList[j];
				if (textRun is ShapedTextRun { Text: { Span: var span } })
				{
					int num3 = 0;
					while (num3 < span.Length)
					{
						char ch = span[num3];
						if (IsSeparator(ch))
						{
							if (!flag && num - num2 > 0)
							{
								double item = MeasureSegmentWidth(rentedList, array, num2, num - num2);
								list.Add((num2, num - num2, item, false));
							}
							double item2 = MeasureSegmentWidth(rentedList, array, num, 1);
							list.Add((num, 1, item2, true));
							num2 = num + 1;
							flag = true;
						}
						else if (flag)
						{
							num2 = num;
							flag = false;
						}
						num3++;
						num++;
					}
				}
				else
				{
					if (flag)
					{
						num2 = num;
						flag = false;
					}
					num += textRun.Length;
				}
			}
			if (num - num2 > 0)
			{
				double item3 = MeasureSegmentWidth(rentedList, array, num2, num - num2);
				list.Add((num2, num - num2, item3, false));
			}
			if (list.Count == 0)
			{
				return null;
			}
			double[] array2 = new double[list.Count + 1];
			for (int k = 0; k < list.Count; k++)
			{
				(int, int, double, bool) tuple = list[k];
				double item4 = tuple.Item3;
				if (!tuple.Item4)
				{
					list2.Add(k);
				}
				array2[k + 1] = array2[k] + item4;
			}
			int num4 = num / 2;
			int num5 = 0;
			long num6 = long.MaxValue;
			for (int l = 0; l < list2.Count; l++)
			{
				(int, int, double, bool) tuple2 = list[list2[l]];
				int item5 = tuple2.Item1;
				int item6 = tuple2.Item2;
				int num7 = Math.Abs(item5 + item6 / 2 - num4);
				if (num7 < num6)
				{
					num6 = num7;
					num5 = l;
				}
			}
			int count = list2.Count;
			FormattingObjectPool.RentedList<TextRun> first;
			FormattingObjectPool.RentedList<TextRun> second;
			if (count > 0)
			{
				for (int m = 1; m <= count; m++)
				{
					int num8 = (m - 1) / 2;
					int num9 = num5 - num8;
					List<int> list3 = new List<int>();
					int num10 = Math.Max(0, num5 - (m - 1));
					int num11 = Math.Min(count - m, num5 + (m - 1));
					for (int num12 = num9; num12 >= num10; num12--)
					{
						list3.Add(num12);
					}
					for (int n = num9 + 1; n <= num11; n++)
					{
						list3.Add(n);
					}
					foreach (int item9 in list3)
					{
						if (item9 < 0 || item9 + m > count)
						{
							continue;
						}
						int index = item9;
						int index2 = item9 + m - 1;
						int num13 = list2[index];
						int num14 = list2[index2];
						int item7 = list[num13].Item1;
						int num15 = num - (list[num14].Item1 + list[num14].Item2);
						if (item7 <= 0 || num15 <= 0)
						{
							continue;
						}
						double num16 = array2[num14 + 1] - array2[num13];
						if (!MathUtilities.LessThanOrClose(width - num16 + shapedTextRun.Size.Width, Width))
						{
							continue;
						}
						int item8 = list[num13].Item1;
						int length = list[num14].Item1 + list[num14].Item2 - item8;
						FormattingObjectPool.RentedList<TextRun> rentedList2 = null;
						FormattingObjectPool.RentedList<TextRun> rentedList3 = null;
						FormattingObjectPool.RentedList<TextRun> rentedList4 = null;
						FormattingObjectPool.RentedList<TextRun> rentedList5 = null;
						try
						{
							TextFormatterImpl.SplitTextRuns(rentedList, item8, instance).Deconstruct(out first, out second);
							rentedList2 = first;
							rentedList3 = second;
							if (rentedList3 == null)
							{
								return null;
							}
							TextFormatterImpl.SplitTextRuns(rentedList3, length, instance).Deconstruct(out second, out first);
							rentedList4 = second;
							rentedList5 = first;
							TextRun[] array3 = new TextRun[(rentedList2?.Count ?? 0) + 1 + (rentedList5?.Count ?? 0)];
							int num17 = 0;
							if (rentedList2 != null)
							{
								foreach (TextRun item10 in rentedList2)
								{
									array3[num17++] = item10;
								}
							}
							array3[num17++] = shapedTextRun;
							if (rentedList5 != null)
							{
								foreach (TextRun item11 in rentedList5)
								{
									array3[num17++] = item11;
								}
							}
							return array3;
						}
						finally
						{
							instance.TextRunLists.Return(ref rentedList2);
							instance.TextRunLists.Return(ref rentedList3);
							instance.TextRunLists.Return(ref rentedList4);
							instance.TextRunLists.Return(ref rentedList5);
						}
					}
				}
			}
			int num18 = 0;
			double num19 = textLine.WidthIncludingTrailingWhitespace;
			for (int num20 = 0; num20 < list.Count; num20++)
			{
				(int, int, double, bool) tuple3 = list[num20];
				if (num20 < list.Count - 1 && MathUtilities.GreaterThan(num19 - tuple3.Item3, Width))
				{
					num19 -= tuple3.Item3;
					num18 += tuple3.Item2;
					continue;
				}
				FormattingObjectPool.RentedList<TextRun> rentedList6 = null;
				FormattingObjectPool.RentedList<TextRun> rentedList7 = null;
				try
				{
					TextFormatterImpl.SplitTextRuns(rentedList, num18, instance).Deconstruct(out first, out second);
					rentedList6 = first;
					rentedList7 = second;
					TextRun textRun2 = null;
					int num21 = 0;
					if (rentedList7 != null && rentedList7.Count > 0)
					{
						num21 = Math.Max(0, rentedList7.Count - 1);
						if (rentedList7[0] is ShapedTextRun shapedTextRun3)
						{
							double availableWidth = Width - shapedTextRun.Size.Width;
							if (shapedTextRun3.TryMeasureCharactersBackwards(availableWidth, out var length2, out var _))
							{
								int num22 = shapedTextRun3.Length - length2;
								if (num22 > 0)
								{
									(_, textRun2) = shapedTextRun3.Split(num22);
								}
								else if (length2 > 0)
								{
									textRun2 = shapedTextRun3;
								}
							}
						}
					}
					TextRun[] array4 = new TextRun[((textRun2 != null) ? 1 : 0) + 1 + num21];
					int num23 = 0;
					array4[num23++] = shapedTextRun;
					if (textRun2 != null)
					{
						array4[num23++] = textRun2;
					}
					if (rentedList7 != null)
					{
						for (int num24 = 1; num24 < rentedList7.Count; num24++)
						{
							TextRun textRun3 = rentedList7[num24];
							array4[num23++] = textRun3;
						}
					}
					return array4;
				}
				finally
				{
					instance.TextRunLists.Return(ref rentedList6);
					instance.TextRunLists.Return(ref rentedList7);
				}
			}
			return null;
		}
		finally
		{
			instance.TextRunLists.Return(ref rentedList);
		}
	}

	private bool IsSeparator(char ch)
	{
		char[] separators = _separators;
		for (int i = 0; i < separators.Length; i++)
		{
			if (separators[i] == ch)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Calculates the total width of a specified segment within a sequence of text runs.
	/// </summary>
	/// <remarks>
	/// Uses the pre-computed <paramref name="runStartChars" /> cumulative-offset table
	/// to binary-search the first overlapping run (O(log N)) instead of re-scanning all
	/// runs from index 0 on every call. For each shaped overlap, delegates to
	/// <see cref="M:Avalonia.Media.TextFormatting.ShapedBuffer.GetCharRangeWidth(System.Int32,System.Int32)" />, which uses the cluster-width cache —
	/// O(log clusters) per call and direction-agnostic (the cache is built in logical
	/// order for both LTR and RTL buffers). Drawable runs are measured as a whole if
	/// they fully overlap the segment, matching the original behavior.
	/// </remarks>
	/// <param name="runs">The collection of text runs to measure.</param>
	/// <param name="runStartChars">Cumulative char-offset table; entry <c>i</c> is the
	/// total length of runs <c>0..i-1</c>, entry <c>Count</c> is the total char length.</param>
	/// <param name="segmentStart">Zero-based start index of the segment, relative to the combined text runs.</param>
	/// <param name="segmentLength">Number of characters in the segment. Must be non-negative.</param>
	/// <returns>The segment width in device-independent units, or 0 if the segment is empty or out of range.</returns>
	private static double MeasureSegmentWidth(IReadOnlyList<TextRun> runs, int[] runStartChars, int segmentStart, int segmentLength)
	{
		if (segmentLength <= 0)
		{
			return 0.0;
		}
		int num = segmentStart + segmentLength;
		int i = FindFirstOverlappingRun(runStartChars, segmentStart);
		double num2 = 0.0;
		for (; i < runs.Count; i++)
		{
			int num3 = runStartChars[i];
			if (num3 >= num)
			{
				break;
			}
			TextRun textRun = runs[i];
			int val = num3 + textRun.Length;
			int num4 = Math.Max(segmentStart, num3);
			int num5 = Math.Min(num, val);
			if (num5 <= num4)
			{
				continue;
			}
			if (!(textRun is ShapedTextRun shapedTextRun))
			{
				if (textRun is DrawableTextRun drawableTextRun && num5 - num4 >= drawableTextRun.Length)
				{
					num2 += drawableTextRun.Size.Width;
				}
			}
			else
			{
				num2 += shapedTextRun.ShapedBuffer.GetCharRangeWidth(num4 - num3, num5 - num3);
			}
		}
		return num2;
	}

	/// <summary>
	/// Binary-search <paramref name="runStartChars" /> for the largest index
	/// <c>i</c> such that <c>runStartChars[i] &lt;= charIndex</c>. That index
	/// is the first run that can contain or precede <paramref name="charIndex" />.
	/// </summary>
	private static int FindFirstOverlappingRun(int[] runStartChars, int charIndex)
	{
		if (charIndex <= 0)
		{
			return 0;
		}
		int num = 0;
		int num2 = runStartChars.Length - 2;
		if (num2 < 0)
		{
			return 0;
		}
		while (num < num2)
		{
			int num3 = num + num2 + 1 >> 1;
			if (runStartChars[num3] <= charIndex)
			{
				num = num3;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		return num;
	}
}
