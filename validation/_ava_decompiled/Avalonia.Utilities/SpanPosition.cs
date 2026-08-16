namespace Avalonia.Utilities;

/// <summary>
/// Represents a Span's position as a pair of related values: its index in the 
/// SpanVector its CP offset from the start of the SpanVector.
/// </summary>
internal readonly struct SpanPosition
{
	internal int Index { get; }

	internal int Offset { get; }

	internal SpanPosition(int spanIndex, int spanOffset)
	{
		Index = spanIndex;
		Offset = spanOffset;
	}
}
