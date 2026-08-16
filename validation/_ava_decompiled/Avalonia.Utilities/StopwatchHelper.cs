using System;
using System.Diagnostics;

namespace Avalonia.Utilities;

/// <summary>
/// Allows using <see cref="T:System.Diagnostics.Stopwatch" /> as timestamps without allocating.
/// </summary>
/// <remarks>Equivalent to Stopwatch.GetElapsedTime in .NET 7.</remarks>
internal static class StopwatchHelper
{
	private static readonly double s_timestampToTicks = 10000000.0 / (double)Stopwatch.Frequency;

	private static readonly double s_timestampToMs = s_timestampToTicks / 10000.0;

	public static TimeSpan GetElapsedTime(long startingTimestamp)
	{
		return GetElapsedTime(startingTimestamp, Stopwatch.GetTimestamp());
	}

	public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
	{
		return new TimeSpan((long)((double)(endingTimestamp - startingTimestamp) * s_timestampToTicks));
	}

	public static double GetElapsedTimeMs(long startingTimestamp)
	{
		return (double)(Stopwatch.GetTimestamp() - startingTimestamp) * s_timestampToMs;
	}
}
