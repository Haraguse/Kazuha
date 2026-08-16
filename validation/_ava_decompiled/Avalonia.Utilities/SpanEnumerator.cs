using System.Collections;

namespace Avalonia.Utilities;

/// <summary>
/// ENUMERATOR: To navigate a vector through its element
/// </summary>
internal sealed class SpanEnumerator : IEnumerator
{
	private readonly SpanVector _spans;

	private int _current;

	/// <summary>
	/// The current span
	/// </summary>
	public object Current => _spans[_current];

	internal SpanEnumerator(SpanVector spans)
	{
		_spans = spans;
		_current = -1;
	}

	/// <summary>
	/// Move to the next span
	/// </summary>
	public bool MoveNext()
	{
		_current++;
		return _current < _spans.Count;
	}

	/// <summary>
	/// Reset the enumerator
	/// </summary>
	public void Reset()
	{
		_current = -1;
	}
}
