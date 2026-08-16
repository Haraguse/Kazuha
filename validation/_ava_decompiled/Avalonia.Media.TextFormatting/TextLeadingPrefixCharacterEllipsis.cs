using System;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Ellipsis based on a fixed length leading prefix and suffix growing from the end at character granularity.
/// </summary>
public sealed class TextLeadingPrefixCharacterEllipsis : TextCollapsingProperties
{
	private readonly int _prefixLength;

	/// <inheritdoc />
	public override double Width { get; }

	/// <inheritdoc />
	public override TextRun Symbol { get; }

	public override FlowDirection FlowDirection { get; }

	/// <summary>
	/// Construct a text trailing word ellipsis collapsing properties.
	/// </summary>
	/// <param name="ellipsis">Text used as collapsing symbol.</param>
	/// <param name="prefixLength">Length of leading prefix.</param>
	/// <param name="width">width in which collapsing is constrained to</param>
	/// <param name="textRunProperties">text run properties of ellipsis symbol</param>
	/// <param name="flowDirection">the flow direction of the collapes line.</param>
	public TextLeadingPrefixCharacterEllipsis(string ellipsis, int prefixLength, double width, TextRunProperties textRunProperties, FlowDirection flowDirection)
	{
		if (prefixLength < 0)
		{
			throw new ArgumentOutOfRangeException("prefixLength");
		}
		_prefixLength = prefixLength;
		Width = width;
		Symbol = new TextCharacters(ellipsis, textRunProperties);
		FlowDirection = flowDirection;
	}

	/// <inheritdoc />
	public override TextRun[]? Collapse(TextLine textLine)
	{
		FormattingObjectPool instance = FormattingObjectPool.Instance;
		FormattingObjectPool.RentedList<TextRun> rentedList = instance.TextRunLists.Rent();
		try
		{
			LogicalTextRunEnumerator logicalTextRunEnumerator = new LogicalTextRunEnumerator(textLine);
			TextRun run;
			while (logicalTextRunEnumerator.MoveNext(out run))
			{
				rentedList.Add(run);
			}
			ShapedTextRun shapedTextRun = TextFormatter.CreateSymbol(Symbol, FlowDirection);
			if (MathUtilities.LessThan(Width, shapedTextRun.GlyphRun.Bounds.Width))
			{
				return Array.Empty<TextRun>();
			}
			double num = Width - shapedTextRun.Size.Width;
			double num2 = num;
			int num3 = 0;
			for (int i = 0; i < rentedList.Count; i++)
			{
				TextRun textRun = rentedList[i];
				if (!(textRun is ShapedTextRun { Size: var size } shapedTextRun2))
				{
					if (textRun is DrawableTextRun drawableTextRun)
					{
						num2 -= drawableTextRun.Size.Width;
					}
				}
				else
				{
					if (MathUtilities.GreaterThan(size.Width, num2))
					{
						shapedTextRun2.TryMeasureCharacters(num2, out var length);
						int num4 = num3 + length;
						if (num4 > 0)
						{
							FormattingObjectPool.RentedList<TextRun> rentedList2 = instance.TextRunLists.Rent();
							FormattingObjectPool.RentedList<TextRun> rentedList3 = null;
							FormattingObjectPool.RentedList<TextRun> rentedList4 = null;
							FormattingObjectPool.RentedList<TextRun> rentedList5 = null;
							try
							{
								int num5 = Math.Min(_prefixLength, num4);
								IReadOnlyList<TextRun> readOnlyList;
								if (num5 > 0)
								{
									(rentedList3, rentedList4) = TextFormatterImpl.SplitTextRuns(rentedList, num5, instance);
									readOnlyList = rentedList4;
									if (rentedList3 != null)
									{
										foreach (TextRun item in rentedList3)
										{
											rentedList2.Add(item);
										}
									}
								}
								else
								{
									readOnlyList = rentedList;
								}
								rentedList2.Add(shapedTextRun);
								if (num4 <= _prefixLength || readOnlyList == null)
								{
									return rentedList2.ToArray();
								}
								double num6 = num;
								if (rentedList3 != null)
								{
									foreach (TextRun item2 in rentedList3)
									{
										if (!(item2 is ShapedTextRun shapedTextRun3))
										{
											if (item2 is DrawableTextRun drawableTextRun2)
											{
												num6 -= drawableTextRun2.Size.Width;
											}
										}
										else
										{
											num6 -= shapedTextRun3.Size.Width;
										}
									}
								}
								rentedList5 = instance.TextRunLists.Rent();
								for (int num7 = readOnlyList.Count - 1; num7 >= 0; num7--)
								{
									TextRun textRun2 = readOnlyList[num7];
									if (textRun2 is ShapedTextRun shapedTextRun4 && shapedTextRun4.TryMeasureCharactersBackwards(num6, out var length2, out var width))
									{
										num6 -= width;
										int num8 = textRun2.Length - length2;
										if (num8 > 0)
										{
											SplitResult<ShapedTextRun> splitResult2 = shapedTextRun4.Split(num8);
											rentedList5.Add(splitResult2.Second);
										}
										else if (length2 > 0)
										{
											rentedList5.Add(shapedTextRun4);
										}
									}
								}
								for (int num9 = rentedList5.Count - 1; num9 >= 0; num9--)
								{
									rentedList2.Add(rentedList5[num9]);
								}
								return rentedList2.ToArray();
							}
							finally
							{
								instance.TextRunLists.Return(ref rentedList3);
								instance.TextRunLists.Return(ref rentedList4);
								instance.TextRunLists.Return(ref rentedList5);
								instance.TextRunLists.Return(ref rentedList2);
							}
						}
						return new TextRun[1] { shapedTextRun };
					}
					num2 -= shapedTextRun2.Size.Width;
				}
				num3 += textRun.Length;
			}
			return null;
		}
		finally
		{
			instance.TextRunLists.Return(ref rentedList);
		}
	}
}
