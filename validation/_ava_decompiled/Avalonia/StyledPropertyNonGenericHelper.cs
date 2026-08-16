using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Avalonia;

/// <summary>
/// Contains methods for <see cref="T:Avalonia.StyledProperty`1" /> that aren't using generic arguments.
/// Separated to avoid unnecessary generic instantiations.
/// </summary>
internal static class StyledPropertyNonGenericHelper
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidValue(string propertyName, object? value, string paramName)
	{
		string value2 = value?.GetType().FullName ?? "(null)";
		throw new ArgumentException($"Invalid value for Property '{propertyName}': '{value}' ({value2})", paramName);
	}

	public static void ThrowInvalidDefaultValue(string propertyName, object? defaultValue, string paramName)
	{
		throw new ArgumentException($"'{defaultValue}' is not a valid default value for '{propertyName}'.", paramName);
	}
}
