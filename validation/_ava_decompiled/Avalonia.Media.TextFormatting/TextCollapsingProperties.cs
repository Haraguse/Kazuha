namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Properties of text collapsing.
/// </summary>
public abstract class TextCollapsingProperties
{
	/// <summary>
	/// Gets the width in which the collapsible range is constrained to.
	/// </summary>
	public abstract double Width { get; }

	/// <summary>
	/// Gets the text run that is used as collapsing symbol.
	/// </summary>
	public abstract TextRun Symbol { get; }

	/// <summary>
	/// Gets the flow direction that is used for collapsing.
	/// </summary>
	public abstract FlowDirection FlowDirection { get; }

	/// <summary>
	/// Collapses the given text line and returns the resulting runs, or
	/// <see langword="null" /> if no collapse is needed (the consumer
	/// then keeps the original line unchanged).
	/// </summary>
	/// <param name="textLine">Text line to collapse.</param>
	/// <remarks>
	/// Implementations MUST return runs in <b>logical order</b>. The
	/// consumer (<c>TextLineImpl.Collapse</c>) wraps the returned array
	/// in a new <see cref="T:Avalonia.Media.TextFormatting.TextLine" /> and re-runs the BiDi reorderer
	/// via <c>FinalizeLine</c>, so pre-applying visual order here would
	/// be reordered a second time and produce garbled output on RTL or
	/// mixed-bidi lines.
	/// <para>
	/// Iterate the source line's runs via
	/// <c>LogicalTextRunEnumerator</c>, not <see cref="P:Avalonia.Media.TextFormatting.TextLine.TextRuns" />
	/// (which is post-bidi visual order). Use
	/// <see cref="M:Avalonia.Media.TextFormatting.TextCollapsingProperties.CreateCollapsedRuns(Avalonia.Media.TextFormatting.TextLine,System.Int32,Avalonia.Media.TextFormatting.TextRun)" /> when an implementation only
	/// needs the standard "logical prefix + symbol" shape.
	/// </para>
	/// </remarks>
	public abstract TextRun[]? Collapse(TextLine textLine);

	/// <summary>
	/// Creates a list of runs for given collapsed length which includes specified symbol at the end.
	/// </summary>
	/// <param name="textLine">The text line.</param>
	/// <param name="collapsedLength">The collapsed length.</param>
	/// <param name="shapedSymbol">The symbol.</param>
	/// <returns>List of remaining runs.</returns>
	public static TextRun[] CreateCollapsedRuns(TextLine textLine, int collapsedLength, TextRun shapedSymbol)
	{
		if (collapsedLength <= 0)
		{
			return new TextRun[1] { shapedSymbol };
		}
		FormattingObjectPool instance = FormattingObjectPool.Instance;
		FormattingObjectPool.RentedList<TextRun> rentedList = null;
		FormattingObjectPool.RentedList<TextRun> rentedList2 = null;
		FormattingObjectPool.RentedList<TextRun> rentedList3 = instance.TextRunLists.Rent();
		try
		{
			LogicalTextRunEnumerator logicalTextRunEnumerator = new LogicalTextRunEnumerator(textLine);
			int num = 0;
			TextRun run;
			while (logicalTextRunEnumerator.MoveNext(out run) && num < collapsedLength)
			{
				num += run.Length;
				rentedList3.Add(run);
			}
			(rentedList, rentedList2) = TextFormatterImpl.SplitTextRuns(rentedList3, collapsedLength, instance);
			TextRun[] array = new TextRun[rentedList.Count + 1];
			rentedList.CopyTo(array);
			array[^1] = shapedSymbol;
			return array;
		}
		finally
		{
			instance.TextRunLists.Return(ref rentedList3);
			instance.TextRunLists.Return(ref rentedList);
			instance.TextRunLists.Return(ref rentedList2);
		}
	}
}
