using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

/// <summary>
/// Helper methods to help inlining methods that do a throw check.
/// </summary>
internal static class ThrowHelper
{
	/// <summary>
	/// Equivalent of .NET6+ ArgumentNullException.ThrowIfNull().
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
	{
		if (argument == null)
		{
			ThrowArgumentNullException(paramName);
		}
		[DoesNotReturn]
		static void ThrowArgumentNullException(string? paramName2)
		{
			throw new ArgumentNullException(paramName2);
		}
	}

	/// <summary>
	/// Equivalent of .NET8+ ArgumentException.ThrowIfNullOrEmpty().
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
	{
		if (string.IsNullOrEmpty(argument))
		{
			ThrowNullOrEmptyException(argument, paramName);
		}
		[DoesNotReturn]
		static void ThrowNullOrEmptyException(string? argument2, string? paramName2)
		{
			ThrowIfNull(argument2, paramName2);
			throw new ArgumentException("Empty string", paramName2);
		}
	}
}
