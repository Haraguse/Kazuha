using System;
using System.Collections.Generic;

namespace Avalonia.Data;

/// <summary>
/// An optional typed value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <remarks>
/// This struct is similar to <see cref="T:System.Nullable`1" /> except it also accepts reference types:
/// note that null is a valid value for reference types. It is also similar to
/// <see cref="T:Avalonia.Data.BindingValue`1" /> but has only two states: "value present" and "value missing".
///
/// To create a new optional value you can:
///
/// - For a simple value, call the <see cref="T:Avalonia.Data.Optional`1" /> constructor or use an implicit
///   conversion from <typeparamref name="T" />
/// - For an missing value, use <see cref="P:Avalonia.Data.Optional`1.Empty" /> or simply `default`
/// </remarks>
public readonly struct Optional<T> : IEquatable<Optional<T>>
{
	private readonly T _value;

	/// <summary>
	/// Gets a value indicating whether a value is present.
	/// </summary>
	public bool HasValue { get; }

	/// <summary>
	/// Gets the value.
	/// </summary>
	/// <exception cref="T:System.InvalidOperationException">
	/// <see cref="P:Avalonia.Data.Optional`1.HasValue" /> is false.
	/// </exception>
	public T Value
	{
		get
		{
			if (!HasValue)
			{
				throw new InvalidOperationException("Optional has no value.");
			}
			return _value;
		}
	}

	/// <summary>
	/// Returns an <see cref="T:Avalonia.Data.Optional`1" /> without a value.
	/// </summary>
	public static Optional<T> Empty => default(Optional<T>);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.Optional`1" /> struct with value.
	/// </summary>
	/// <param name="value">The value.</param>
	public Optional(T value)
	{
		_value = value;
		HasValue = true;
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		if (obj is Optional<T> optional)
		{
			return this == optional;
		}
		return false;
	}

	/// <inheritdoc />
	public bool Equals(Optional<T> other)
	{
		return this == other;
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		if (!HasValue)
		{
			return 0;
		}
		T value = _value;
		if (value == null)
		{
			return 0;
		}
		return value.GetHashCode();
	}

	/// <summary>
	/// Casts the value (if any) to an <see cref="T:System.Object" />.
	/// </summary>
	/// <returns>The cast optional value.</returns>
	public Optional<object?> ToObject()
	{
		if (!HasValue)
		{
			return default(Optional<object>);
		}
		return new Optional<object>(_value);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		if (!HasValue)
		{
			return "(empty)";
		}
		T value = _value;
		return ((value != null) ? value.ToString() : null) ?? "(null)";
	}

	/// <summary>
	/// Gets the value if present, otherwise the default value.
	/// </summary>
	/// <returns>The value.</returns>
	public T? GetValueOrDefault()
	{
		return _value;
	}

	/// <summary>
	/// Gets the value if present, otherwise a default value.
	/// </summary>
	/// <param name="defaultValue">The default value.</param>
	/// <returns>The value.</returns>
	public T? GetValueOrDefault(T defaultValue)
	{
		if (!HasValue)
		{
			return defaultValue;
		}
		return _value;
	}

	/// <summary>
	/// Gets the value if present, otherwise the default value.
	/// </summary>
	/// <returns>
	/// The value if present and of the correct type, `default(TResult)` if the value is
	/// not present or of an incorrect type.
	/// </returns>
	public TResult? GetValueOrDefault<TResult>()
	{
		if (!HasValue)
		{
			return default(TResult);
		}
		T value = _value;
		if (value is TResult)
		{
			return (TResult)((((object)value) is TResult) ? ((object)value) : null);
		}
		return default(TResult);
	}

	/// <summary>
	/// Gets the value if present, otherwise a default value.
	/// </summary>
	/// <param name="defaultValue">The default value.</param>
	/// <returns>
	/// The value if present and of the correct type, `default(TResult)` if the value is
	/// present but not of the correct type or null, or <paramref name="defaultValue" /> if the
	/// value is not present.
	/// </returns>
	public TResult? GetValueOrDefault<TResult>(TResult defaultValue)
	{
		if (!HasValue)
		{
			return defaultValue;
		}
		T value = _value;
		if (value is TResult)
		{
			return (TResult)((((object)value) is TResult) ? ((object)value) : null);
		}
		return default(TResult);
	}

	/// <summary>
	/// Creates an <see cref="T:Avalonia.Data.Optional`1" /> from an instance of the underlying value type.
	/// </summary>
	/// <param name="value">The value.</param>
	public static implicit operator Optional<T>(T value)
	{
		return new Optional<T>(value);
	}

	/// <summary>
	/// Compares two <see cref="T:Avalonia.Data.Optional`1" />s for inequality.
	/// </summary>
	/// <param name="x">The first value.</param>
	/// <param name="y">The second value.</param>
	/// <returns>True if the values are unequal; otherwise false.</returns>
	public static bool operator !=(Optional<T> x, Optional<T> y)
	{
		return !(x == y);
	}

	/// <summary>
	/// Compares two <see cref="T:Avalonia.Data.Optional`1" />s for equality.
	/// </summary>
	/// <param name="x">The first value.</param>
	/// <param name="y">The second value.</param>
	/// <returns>True if the values are equal; otherwise false.</returns>
	public static bool operator ==(Optional<T> x, Optional<T> y)
	{
		if (!x.HasValue && !y.HasValue)
		{
			return true;
		}
		if (x.HasValue && y.HasValue)
		{
			return EqualityComparer<T>.Default.Equals(x.Value, y.Value);
		}
		return false;
	}
}
