namespace Avalonia.Data;

public static class OptionalExtensions
{
	/// <summary>
	/// Casts the type of an <see cref="T:Avalonia.Data.Optional`1" /> using only the C# cast operator.
	/// </summary>
	/// <typeparam name="T">The target type.</typeparam>
	/// <param name="value">The binding value.</param>
	/// <returns>The cast value.</returns>
	public static Optional<T> Cast<T>(this Optional<object?> value)
	{
		if (!value.HasValue)
		{
			return Optional<T>.Empty;
		}
		return new Optional<T>((T)value.Value);
	}
}
