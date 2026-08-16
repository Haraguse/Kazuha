namespace Avalonia.Utilities;

/// <summary>
/// RIDER: To navigate a vector through character index
/// </summary>
internal struct SpanRider
{
	private readonly SpanVector _spans;

	private SpanPosition _spanPosition;

	/// <summary>
	/// The first cp of the current span
	/// </summary>
	public int CurrentSpanStart => _spanPosition.Offset;

	/// <summary>
	/// The length of current span start from the current cp
	/// </summary>
	public int Length { get; private set; }

	/// <summary>
	/// The current position
	/// </summary>
	public int CurrentPosition { get; private set; }

	/// <summary>
	/// The element of the current span
	/// </summary>
	public object? CurrentElement
	{
		get
		{
			if (_spanPosition.Index < _spans.Count)
			{
				return _spans[_spanPosition.Index].element;
			}
			return _spans.Default;
		}
	}

	/// <summary>
	/// Index of the span at the current position.
	/// </summary>
	public int CurrentSpanIndex => _spanPosition.Index;

	/// <summary>
	/// Index and first cp of the current span.
	/// </summary>
	public SpanPosition SpanPosition => _spanPosition;

	public SpanRider(SpanVector spans, SpanPosition latestPosition)
		: this(spans, latestPosition, latestPosition.Offset)
	{
	}

	public SpanRider(SpanVector spans, SpanPosition latestPosition = default(SpanPosition), int cp = 0)
	{
		_spans = spans;
		_spanPosition = default(SpanPosition);
		CurrentPosition = 0;
		Length = 0;
		At(latestPosition, cp);
	}

	/// <summary>
	/// Move rider to a given cp
	/// </summary>
	public bool At(int cp)
	{
		return At(_spanPosition, cp);
	}

	public bool At(SpanPosition latestPosition, int cp)
	{
		bool num = _spans.FindSpan(cp, latestPosition, out _spanPosition);
		if (num)
		{
			Length = _spans[_spanPosition.Index].length - (cp - _spanPosition.Offset);
			CurrentPosition = cp;
			return num;
		}
		Length = int.MaxValue;
		CurrentPosition = _spanPosition.Offset;
		return num;
	}
}
