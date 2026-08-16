using System;

namespace Avalonia.Data;

/// <summary>
/// Represents a binding notification that can be a valid binding value, or a binding or
/// data validation error.
/// </summary>
/// <remarks>
/// This class is very similar to <see cref="T:Avalonia.Data.BindingValue`1" />, but where <see cref="T:Avalonia.Data.BindingValue`1" />
/// is used by typed bindings, this class is used to hold binding and data validation errors in
/// untyped bindings. As Avalonia moves towards using typed bindings by default we may want to remove
/// this class.
/// </remarks>
public class BindingNotification
{
	/// <summary>
	/// A binding notification representing the null value.
	/// </summary>
	public static readonly BindingNotification Null = new BindingNotification(null);

	/// <summary>
	/// A binding notification representing <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />.
	/// </summary>
	public static readonly BindingNotification UnsetValue = new BindingNotification(AvaloniaProperty.UnsetValue);

	private object? _value;

	/// <summary>
	/// Gets the value that should be passed to the target when <see cref="P:Avalonia.Data.BindingNotification.HasValue" />
	/// is true.
	/// </summary>
	/// <remarks>
	/// If this property is read when <see cref="P:Avalonia.Data.BindingNotification.HasValue" /> is false then it will return
	/// <see cref="F:Avalonia.AvaloniaProperty.UnsetValue" />.
	/// </remarks>
	public object? Value => _value;

	/// <summary>
	/// Gets a value indicating whether <see cref="P:Avalonia.Data.BindingNotification.Value" /> should be pushed to the target.
	/// </summary>
	public bool HasValue => _value != AvaloniaProperty.UnsetValue;

	/// <summary>
	/// Gets the error that occurred on the source, if any.
	/// </summary>
	public Exception? Error { get; set; }

	/// <summary>
	/// Gets the type of error that <see cref="P:Avalonia.Data.BindingNotification.Error" /> represents, if any.
	/// </summary>
	public BindingErrorType ErrorType { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.BindingNotification" /> class.
	/// </summary>
	/// <param name="value">The binding value.</param>
	public BindingNotification(object? value)
	{
		_value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.BindingNotification" /> class.
	/// </summary>
	/// <param name="error">The binding error.</param>
	/// <param name="errorType">The type of the binding error.</param>
	public BindingNotification(Exception error, BindingErrorType errorType)
	{
		if (errorType == BindingErrorType.None)
		{
			throw new ArgumentException("'errorType' may not be None");
		}
		Error = error;
		ErrorType = errorType;
		_value = AvaloniaProperty.UnsetValue;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Data.BindingNotification" /> class.
	/// </summary>
	/// <param name="error">The binding error.</param>
	/// <param name="errorType">The type of the binding error.</param>
	/// <param name="fallbackValue">The fallback value.</param>
	public BindingNotification(Exception error, BindingErrorType errorType, object? fallbackValue)
		: this(error, errorType)
	{
		_value = fallbackValue;
	}

	/// <summary>
	/// Compares two instances of <see cref="T:Avalonia.Data.BindingNotification" /> for equality.
	/// </summary>
	/// <param name="a">The first instance.</param>
	/// <param name="b">The second instance.</param>
	/// <returns>true if the two instances are equal; otherwise false.</returns>
	public static bool operator ==(BindingNotification? a, BindingNotification? b)
	{
		if ((object)a == b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		if (a.HasValue == b.HasValue && a.ErrorType == b.ErrorType && (!a.HasValue || object.Equals(a.Value, b.Value)))
		{
			if (a.ErrorType != BindingErrorType.None)
			{
				return ExceptionEquals(a.Error, b.Error);
			}
			return true;
		}
		return false;
	}

	/// <summary>
	/// Compares two instances of <see cref="T:Avalonia.Data.BindingNotification" /> for inequality.
	/// </summary>
	/// <param name="a">The first instance.</param>
	/// <param name="b">The second instance.</param>
	/// <returns>true if the two instances are unequal; otherwise false.</returns>
	public static bool operator !=(BindingNotification? a, BindingNotification? b)
	{
		return !(a == b);
	}

	/// <summary>
	/// Gets a value from an object that may be a <see cref="T:Avalonia.Data.BindingNotification" />.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <returns>The value.</returns>
	/// <remarks>
	/// If <paramref name="o" /> is a <see cref="T:Avalonia.Data.BindingNotification" /> then returns the binding
	/// notification's <see cref="P:Avalonia.Data.BindingNotification.Value" />. If not, returns the object unchanged.
	/// </remarks>
	public static object? ExtractValue(object? o)
	{
		if (!(o is BindingNotification bindingNotification))
		{
			return o;
		}
		return bindingNotification.Value;
	}

	/// <summary>
	/// Updates the value of an object that may be a <see cref="T:Avalonia.Data.BindingNotification" />.
	/// </summary>
	/// <param name="o">The object that may be a binding notification.</param>
	/// <param name="value">The new value.</param>
	/// <returns>
	/// The updated binding notification if <paramref name="o" /> is a binding notification;
	/// otherwise <paramref name="value" />.
	/// </returns>
	/// <remarks>
	/// If <paramref name="o" /> is a <see cref="T:Avalonia.Data.BindingNotification" /> then sets its value
	/// to <paramref name="value" />. If <paramref name="value" /> is a
	/// <see cref="T:Avalonia.Data.BindingNotification" /> then the value will first be extracted.
	/// </remarks>
	public static object? UpdateValue(object? o, object value)
	{
		if (o is BindingNotification bindingNotification)
		{
			bindingNotification.SetValue(ExtractValue(value));
			return bindingNotification;
		}
		return value;
	}

	/// <summary>
	/// Gets an exception from an object that may be a <see cref="T:Avalonia.Data.BindingNotification" />.
	/// </summary>
	/// <param name="o">The object.</param>
	/// <returns>The value.</returns>
	/// <remarks>
	/// If <paramref name="o" /> is a <see cref="T:Avalonia.Data.BindingNotification" /> then returns the binding
	/// notification's <see cref="P:Avalonia.Data.BindingNotification.Error" />. If not, returns the object unchanged.
	/// </remarks>
	public static object? ExtractError(object? o)
	{
		if (!(o is BindingNotification bindingNotification))
		{
			return o;
		}
		return bindingNotification.Error;
	}

	/// <summary>
	/// Compares an object to an instance of <see cref="T:Avalonia.Data.BindingNotification" /> for equality.
	/// </summary>
	/// <param name="obj">The object to compare.</param>
	/// <returns>true if the two instances are equal; otherwise false.</returns>
	public override bool Equals(object? obj)
	{
		return Equals(obj as BindingNotification);
	}

	/// <summary>
	/// Compares a value to an instance of <see cref="T:Avalonia.Data.BindingNotification" /> for equality.
	/// </summary>
	/// <param name="other">The value to compare.</param>
	/// <returns>true if the two instances are equal; otherwise false.</returns>
	public bool Equals(BindingNotification? other)
	{
		return this == other;
	}

	/// <summary>
	/// Gets the hash code for this instance of <see cref="T:Avalonia.Data.BindingNotification" />. 
	/// </summary>
	/// <returns>A hash code.</returns>
	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	/// <summary>
	/// Adds an error to the <see cref="T:Avalonia.Data.BindingNotification" />.
	/// </summary>
	/// <param name="e">The error to add.</param>
	/// <param name="type">The error type.</param>
	public void AddError(Exception e, BindingErrorType type)
	{
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		if (type == BindingErrorType.None)
		{
			throw new ArgumentException("BindingErrorType may not be None", "type");
		}
		Error = ((Error != null) ? new AggregateException(Error, e) : e);
		if (type == BindingErrorType.Error || ErrorType == BindingErrorType.Error)
		{
			ErrorType = BindingErrorType.Error;
		}
	}

	/// <summary>
	/// Removes the <see cref="P:Avalonia.Data.BindingNotification.Value" /> and makes <see cref="P:Avalonia.Data.BindingNotification.HasValue" /> return null.
	/// </summary>
	public void ClearValue()
	{
		_value = AvaloniaProperty.UnsetValue;
	}

	/// <summary>
	/// Sets the <see cref="P:Avalonia.Data.BindingNotification.Value" />.
	/// </summary>
	public void SetValue(object? value)
	{
		_value = value;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		if (ErrorType != BindingErrorType.None)
		{
			if (HasValue)
			{
				return $"{{{ErrorType}: {Error}, Fallback: {Value}}}";
			}
			return $"{{{ErrorType}: {Error}}}";
		}
		return $"{{Value: {Value}}}";
	}

	private static bool ExceptionEquals(Exception? a, Exception? b)
	{
		if (a?.GetType() == b?.GetType())
		{
			return a?.Message == b?.Message;
		}
		return false;
	}
}
