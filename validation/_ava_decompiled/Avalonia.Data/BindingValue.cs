using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia.Data;

/// <summary>
/// A value passed into a binding.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <remarks>
/// The avalonia binding system is typed, and as such additional state is stored in this
/// structure. A binding value can be in a number of states, described by the
/// <see cref="P:Avalonia.Data.BindingValue`1.Type" /> property:
///
/// - <see cref="F:Avalonia.Data.BindingValueType.Value" />: a simple value
/// - <see cref="F:Avalonia.Data.BindingValueType.UnsetValue" />: the target property will revert to its unbound
///   state until a new binding value is produced. Represented by
///   <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" /> in an untyped context
/// - <see cref="F:Avalonia.Data.BindingValueType.DoNothing" />: the binding value will be ignored. Represented
///   by <see cref="F:Avalonia.Data.BindingOperations.DoNothing" /> in an untyped context
/// - <see cref="F:Avalonia.Data.BindingValueType.BindingError" />: a binding error, such as a missing source
///   property, with an optional fallback value
/// - <see cref="F:Avalonia.Data.BindingValueType.DataValidationError" />: a data validation error, with an
///   optional fallback value
///
/// To create a new binding value you can:
///
/// - For a simple value, call the <see cref="T:Avalonia.Data.BindingValue`1" /> constructor or use an implicit
///   conversion from <typeparamref name="T" />
/// - For an unset value, use <see cref="P:Avalonia.Data.BindingValue`1.Unset" /> or simply `default`
/// - For other types, call one of the static factory methods
/// </remarks>
public readonly record struct BindingValue<T>
{
	/// <summary>
	/// Gets a value indicating whether the binding value represents either a binding or data
	/// validation error.
	/// </summary>
	public bool HasError => Type.HasAllFlags(BindingValueType.HasError);

	/// <summary>
	/// Gets a value indicating whether the binding value has a value.
	/// </summary>
	public bool HasValue => Type.HasAllFlags(BindingValueType.HasValue);

	/// <summary>
	/// Gets the type of the binding value.
	/// </summary>
	public BindingValueType Type { get; }

	/// <summary>
	/// Gets the binding value or fallback value.
	/// </summary>
	/// <exception cref="T:System.InvalidOperationException">
	/// <see cref="P:Avalonia.Data.BindingValue`1.HasValue" /> is false.
	/// </exception>
	public T Value
	{
		get
		{
			if (!HasValue)
			{
				throw new InvalidOperationException("BindingValue has no value.");
			}
			return _value;
		}
	}

	/// <summary>
	/// Gets the binding or data validation error.
	/// </summary>
	public Exception? Error { get; }

	/// <summary>
	/// Returns a binding value with a type of <see cref="F:Avalonia.Data.BindingValueType.UnsetValue" />.
	/// </summary>
	public static BindingValue<T> Unset => new BindingValue<T>(BindingValueType.UnsetValue, default(T), null);

	/// <summary>
	/// Returns a binding value with a type of <see cref="F:Avalonia.Data.BindingValueType.DoNothing" />.
	/// </summary>
	public static BindingValue<T> DoNothing => new BindingValue<T>(BindingValueType.DoNothing, default(T), null);

	private readonly T _value;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.BindingValue`1" /> struct with a type of
	/// <see cref="F:Avalonia.Data.BindingValueType.Value" />
	/// </summary>
	/// <param name="value">The value.</param>
	public BindingValue(T value)
	{
		_value = value;
		Type = BindingValueType.Value;
		Error = null;
	}

	private BindingValue(BindingValueType type, T? value, Exception? error)
	{
		_value = value;
		Type = type;
		Error = error;
	}

	/// <summary>
	/// Converts the binding value to an <see cref="T:Avalonia.Data.Optional`1" />.
	/// </summary>
	/// <returns></returns>
	public Optional<T> ToOptional()
	{
		if (!HasValue)
		{
			return default(Optional<T>);
		}
		return new Optional<T>(_value);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		object obj;
		if (!HasError)
		{
			T value = _value;
			obj = ((value != null) ? value.ToString() : null);
			if (obj == null)
			{
				return "(null)";
			}
		}
		else
		{
			obj = "Error: " + Error.Message;
		}
		return (string)obj;
	}

	/// <summary>
	/// Converts the value to untyped representation, using <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />,
	/// <see cref="F:Avalonia.Data.BindingOperations.DoNothing" /> and <see cref="T:Avalonia.Data.BindingNotification" /> where
	/// appropriate.
	/// </summary>
	/// <returns>The untyped representation of the binding value.</returns>
	public object? ToUntyped()
	{
		return Type switch
		{
			BindingValueType.UnsetValue => AvaloniaProperty.UnsetValue, 
			BindingValueType.DoNothing => BindingOperations.DoNothing, 
			BindingValueType.Value => _value, 
			BindingValueType.BindingError => new BindingNotification(Error, BindingErrorType.Error), 
			BindingValueType.BindingErrorWithFallback => new BindingNotification(Error, BindingErrorType.Error, Value), 
			BindingValueType.DataValidationError => new BindingNotification(Error, BindingErrorType.DataValidationError), 
			BindingValueType.DataValidationErrorWithFallback => new BindingNotification(Error, BindingErrorType.DataValidationError, Value), 
			_ => throw new NotSupportedException("Invalid BindingValueType."), 
		};
	}

	/// <summary>
	/// Returns a new binding value with the specified value.
	/// </summary>
	/// <param name="value">The new value.</param>
	/// <returns>The new binding value.</returns>
	/// <exception cref="T:System.InvalidOperationException">
	/// The binding type is <see cref="F:Avalonia.Data.BindingValueType.UnsetValue" /> or
	/// <see cref="F:Avalonia.Data.BindingValueType.DoNothing" />.
	/// </exception>
	public BindingValue<T> WithValue(T value)
	{
		if (Type == BindingValueType.DoNothing)
		{
			throw new InvalidOperationException("Cannot add value to DoNothing binding value.");
		}
		return new BindingValue<T>(((Type == BindingValueType.UnsetValue) ? BindingValueType.Value : Type) | BindingValueType.HasValue, value, Error);
	}

	/// <summary>
	/// Gets the value of the binding value if present, otherwise the default value.
	/// </summary>
	/// <returns>The value.</returns>
	public T? GetValueOrDefault()
	{
		if (!HasValue)
		{
			return default(T);
		}
		return _value;
	}

	/// <summary>
	/// Gets the value of the binding value if present, otherwise a default value.
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
	/// Gets the value of the binding value if present, otherwise a default value.
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
	/// Creates a <see cref="T:Avalonia.Data.BindingValue`1" /> from an object, handling the special values
	/// <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />, <see cref="F:Avalonia.Data.BindingOperations.DoNothing" /> and
	/// <see cref="T:Avalonia.Data.BindingNotification" />.
	/// </summary>
	/// <param name="value">The untyped value.</param>
	/// <returns>The typed binding value.</returns>
	[RequiresUnreferencedCode("Implicit conversion methods are required for type conversion.")]
	public static BindingValue<T> FromUntyped(object? value)
	{
		return FromUntyped(value, typeof(T));
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Data.BindingValue`1" /> from an object, handling the special values
	/// <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />, <see cref="F:Avalonia.Data.BindingOperations.DoNothing" /> and
	/// <see cref="T:Avalonia.Data.BindingNotification" />.
	/// </summary>
	/// <param name="value">The untyped value.</param>
	/// <param name="targetType">The runtime target type.</param>
	/// <returns>The typed binding value.</returns>
	[RequiresUnreferencedCode("Implicit conversion methods are required for type conversion.")]
	public static BindingValue<T> FromUntyped(object? value, Type targetType)
	{
		if (value == AvaloniaProperty.UnsetValue)
		{
			return Unset;
		}
		if (value == BindingOperations.DoNothing)
		{
			return DoNothing;
		}
		BindingValueType bindingValueType = BindingValueType.Value;
		T value2 = default(T);
		Exception ex = null;
		List<Exception> list = null;
		if (value is BindingNotification bindingNotification)
		{
			ex = bindingNotification.Error;
			bindingValueType = bindingNotification.ErrorType switch
			{
				BindingErrorType.Error => BindingValueType.BindingError, 
				BindingErrorType.DataValidationError => BindingValueType.DataValidationError, 
				_ => BindingValueType.Value, 
			};
			if (bindingNotification.HasValue)
			{
				bindingValueType |= BindingValueType.HasValue;
			}
			value = bindingNotification.Value;
		}
		if ((bindingValueType & BindingValueType.HasValue) != BindingValueType.UnsetValue)
		{
			if (TypeUtilities.TryConvertImplicit(targetType, value, out object result))
			{
				value2 = (T)result;
			}
			else
			{
				InvalidCastException ex2 = new InvalidCastException($"Unable to convert object '{value ?? "(null)"}' of type '{value?.GetType()}' to type '{targetType}'.");
				if (ex == null)
				{
					ex = ex2;
				}
				else
				{
					if (list == null)
					{
						list = new List<Exception> { ex };
					}
					list.Add(ex2);
				}
				bindingValueType = BindingValueType.BindingError;
			}
		}
		if (list != null)
		{
			ex = new AggregateException(list);
		}
		return new BindingValue<T>(bindingValueType, value2, ex);
	}

	/// <summary>
	/// Creates a binding value from an instance of the underlying value type.
	/// </summary>
	/// <param name="value">The value.</param>
	public static implicit operator BindingValue<T>(T value)
	{
		return new BindingValue<T>(value);
	}

	/// <summary>
	/// Creates a binding value from an <see cref="T:Avalonia.Data.Optional`1" />.
	/// </summary>
	/// <param name="optional">The optional value.</param>
	public static implicit operator BindingValue<T>(Optional<T> optional)
	{
		if (!optional.HasValue)
		{
			return Unset;
		}
		return optional.Value;
	}

	/// <summary>
	/// Returns a binding value with a type of <see cref="F:Avalonia.Data.BindingValueType.BindingError" />.
	/// </summary>
	/// <param name="e">The binding error.</param>
	public static BindingValue<T> BindingError(Exception e)
	{
		e = e ?? throw new ArgumentNullException("e");
		return new BindingValue<T>(BindingValueType.BindingError, default(T), e);
	}

	/// <summary>
	/// Returns a binding value with a type of <see cref="F:Avalonia.Data.BindingValueType.BindingErrorWithFallback" />.
	/// </summary>
	/// <param name="e">The binding error.</param>
	/// <param name="fallbackValue">The fallback value.</param>
	public static BindingValue<T> BindingError(Exception e, T fallbackValue)
	{
		e = e ?? throw new ArgumentNullException("e");
		return new BindingValue<T>(BindingValueType.BindingErrorWithFallback, fallbackValue, e);
	}

	/// <summary>
	/// Returns a binding value with a type of <see cref="F:Avalonia.Data.BindingValueType.BindingError" /> or
	/// <see cref="F:Avalonia.Data.BindingValueType.BindingErrorWithFallback" />.
	/// </summary>
	/// <param name="e">The binding error.</param>
	/// <param name="fallbackValue">The fallback value.</param>
	public static BindingValue<T> BindingError(Exception e, Optional<T> fallbackValue)
	{
		e = e ?? throw new ArgumentNullException("e");
		return new BindingValue<T>(fallbackValue.HasValue ? BindingValueType.BindingErrorWithFallback : BindingValueType.BindingError, (T?)(fallbackValue.HasValue ? ((object)fallbackValue.Value) : ((object)default(T))), e);
	}

	/// <summary>
	/// Returns a binding value with a type of <see cref="F:Avalonia.Data.BindingValueType.DataValidationError" />.
	/// </summary>
	/// <param name="e">The data validation error.</param>
	public static BindingValue<T> DataValidationError(Exception e)
	{
		e = e ?? throw new ArgumentNullException("e");
		return new BindingValue<T>(BindingValueType.DataValidationError, default(T), e);
	}

	/// <summary>
	/// Returns a binding value with a type of <see cref="F:Avalonia.Data.BindingValueType.DataValidationErrorWithFallback" />.
	/// </summary>
	/// <param name="e">The data validation error.</param>
	/// <param name="fallbackValue">The fallback value.</param>
	public static BindingValue<T> DataValidationError(Exception e, T fallbackValue)
	{
		e = e ?? throw new ArgumentNullException("e");
		return new BindingValue<T>(BindingValueType.DataValidationErrorWithFallback, fallbackValue, e);
	}

	/// <summary>
	/// Returns a binding value with a type of <see cref="F:Avalonia.Data.BindingValueType.DataValidationError" /> or
	/// <see cref="F:Avalonia.Data.BindingValueType.DataValidationErrorWithFallback" />.
	/// </summary>
	/// <param name="e">The binding error.</param>
	/// <param name="fallbackValue">The fallback value.</param>
	public static BindingValue<T> DataValidationError(Exception e, Optional<T> fallbackValue)
	{
		e = e ?? throw new ArgumentNullException("e");
		return new BindingValue<T>(fallbackValue.HasValue ? BindingValueType.DataValidationErrorWithFallback : BindingValueType.DataValidationError, (T?)(fallbackValue.HasValue ? ((object)fallbackValue.Value) : ((object)default(T))), e);
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Data.BindingValue`1" /> from an object, handling the special values
	/// <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />, <see cref="F:Avalonia.Data.BindingOperations.DoNothing" /> and
	/// <see cref="T:Avalonia.Data.BindingNotification" /> without type conversion.
	/// </summary>
	/// <param name="value">The untyped value.</param>
	/// <returns>The typed binding value.</returns>
	internal static BindingValue<T> FromUntypedStrict(object? value)
	{
		if (value == AvaloniaProperty.UnsetValue)
		{
			return Unset;
		}
		if (value == BindingOperations.DoNothing)
		{
			return DoNothing;
		}
		BindingValueType bindingValueType = BindingValueType.Value;
		T value2 = default(T);
		Exception error = null;
		if (value is BindingNotification bindingNotification)
		{
			error = bindingNotification.Error;
			bindingValueType = bindingNotification.ErrorType switch
			{
				BindingErrorType.Error => BindingValueType.BindingError, 
				BindingErrorType.DataValidationError => BindingValueType.DataValidationError, 
				_ => BindingValueType.Value, 
			};
			if (bindingNotification.HasValue)
			{
				bindingValueType |= BindingValueType.HasValue;
			}
			value = bindingNotification.Value;
		}
		if ((bindingValueType & BindingValueType.HasValue) != BindingValueType.UnsetValue)
		{
			value2 = (T)value;
		}
		return new BindingValue<T>(bindingValueType, value2, error);
	}

	[Conditional("DEBUG")]
	private static void ValidateValue(T value)
	{
		if (value is UnsetValueType)
		{
			throw new InvalidOperationException("AvaloniaProperty.UnsetValue is not a valid value for BindingValue<>.");
		}
		if (value is DoNothingType)
		{
			throw new InvalidOperationException("BindingOperations.DoNothing is not a valid value for BindingValue<>.");
		}
		if (value is BindingValue<object>)
		{
			throw new InvalidOperationException("BindingValue<object> cannot be wrapped in a BindingValue<>.");
		}
	}

	[CompilerGenerated]
	public override int GetHashCode()
	{
		return (EqualityComparer<T>.Default.GetHashCode(_value) * -1521134295 + EqualityComparer<BindingValueType>.Default.GetHashCode(Type)) * -1521134295 + EqualityComparer<Exception>.Default.GetHashCode(Error);
	}

	[CompilerGenerated]
	public bool Equals(BindingValue<T> other)
	{
		if (EqualityComparer<T>.Default.Equals(_value, other._value) && EqualityComparer<BindingValueType>.Default.Equals(Type, other.Type))
		{
			return EqualityComparer<Exception>.Default.Equals(Error, other.Error);
		}
		return false;
	}
}
