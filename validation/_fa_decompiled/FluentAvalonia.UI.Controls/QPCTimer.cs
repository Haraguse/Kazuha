using System.Diagnostics;

namespace FluentAvalonia.UI.Controls;

internal class QPCTimer
{
	private readonly Stopwatch _stopwatch = new Stopwatch();

	public void Reset()
	{
		_stopwatch.Restart();
	}

	public long DurationInMilliseconds()
	{
		return _stopwatch.ElapsedMilliseconds;
	}
}
