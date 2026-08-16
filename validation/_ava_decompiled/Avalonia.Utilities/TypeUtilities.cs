using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

/// <summary>
/// Provides utilities for working with types at runtime.
/// </summary>
public static class TypeUtilities
{
	[Flags]
	internal enum OperatorType
	{
		Implicit = 1,
		Explicit = 2
	}

	private static readonly int[] Conversions = new int[15]
	{
		24573, 17406, 24575, 24575, 24575, 24575, 24575, 24575, 24575, 24575,
		24573, 24573, 24573, 24576, 32767
	};

	private static readonly int[] ImplicitConversions = new int[15]
	{
		1, 7650, 7508, 8184, 7504, 8160, 7488, 8064, 7424, 7680,
		3072, 2048, 4096, 8192, 16384
	};

	private static readonly Type[] InbuiltTypes = new Type[15]
	{
		typeof(bool),
		typeof(char),
		typeof(sbyte),
		typeof(byte),
		typeof(short),
		typeof(ushort),
		typeof(int),
		typeof(uint),
		typeof(long),
		typeof(ulong),
		typeof(float),
		typeof(double),
		typeof(decimal),
		typeof(DateTime),
		typeof(string)
	};

	private static readonly Type[] NumericTypes = new Type[11]
	{
		typeof(byte),
		typeof(decimal),
		typeof(double),
		typeof(short),
		typeof(int),
		typeof(long),
		typeof(sbyte),
		typeof(float),
		typeof(ushort),
		typeof(uint),
		typeof(ulong)
	};

	/// <summary>
	/// Returns a value indicating whether null can be assigned to the specified type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>True if the type accepts null values; otherwise false.</returns>
	public static bool AcceptsNull(Type type)
	{
		if (type.IsValueType)
		{
			return IsNullableType(type);
		}
		return true;
	}

	/// <summary>
	/// Returns a value indicating whether null can be assigned to the specified type.
	/// </summary>
	/// <typeparam name="T">The type</typeparam>
	/// <returns>True if the type accepts null values; otherwise false.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool AcceptsNull<T>()
	{
		return default(T) == null;
	}

	/// <summary>
	/// Returns a value indicating whether value can be casted to the specified type.
	/// If value is null, checks if instances of that type can be null.
	/// </summary>
	/// <typeparam name="T">The type to cast to</typeparam>
	/// <param name="value">The value to check if cast possible</param>
	/// <returns>True if the cast is possible, otherwise false.</returns>
	public static bool CanCast<T>(object? value)
	{
		if (!(value is T))
		{
			if (value == null)
			{
				return AcceptsNull<T>();
			}
			return false;
		}
		return true;
	}

	/// <summary>
	/// Try to convert a value to a type by any means possible.
	/// </summary>
	/// <param name="to">The type to convert to.</param>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">The culture to use.</param>
	/// <param name="result">If successful, contains the convert value.</param>
	/// <returns>True if the cast was successful, otherwise false.</returns>
	[RequiresUnreferencedCode("Conversion methods are required for type conversion, including op_Implicit, op_Explicit, Parse and TypeConverter.")]
	public static bool TryConvert(Type to, object? value, CultureInfo? culture, out object? result)
	{
		if (to == typeof(object))
		{
			result = value;
			return true;
		}
		if (value == null)
		{
			result = null;
			return AcceptsNull(to);
		}
		if (value == AvaloniaProperty.UnsetValue)
		{
			result = value;
			return true;
		}
		Type type = Nullable.GetUnderlyingType(to) ?? to;
		Type type2 = value.GetType();
		if (type.IsAssignableFrom(type2))
		{
			result = value;
			return true;
		}
		if (type == typeof(string))
		{
			result = Convert.ToString(value, culture);
			return true;
		}
		if (type.IsEnum && type2 == typeof(string) && Enum.IsDefined(type, (string)value))
		{
			result = Enum.Parse(type, (string)value);
			return true;
		}
		if (!type2.IsEnum && type.IsEnum)
		{
			result = null;
			if (TryConvert(Enum.GetUnderlyingType(type), value, culture, out object result2))
			{
				result = Enum.ToObject(type, result2);
				return true;
			}
		}
		if (type2.IsEnum && IsNumeric(type))
		{
			try
			{
				result = Convert.ChangeType((int)value, type, culture);
				return true;
			}
			catch
			{
				result = null;
				return false;
			}
		}
		int num = Array.IndexOf(InbuiltTypes, type2);
		int num2 = Array.IndexOf(InbuiltTypes, type);
		if (num != -1 && num2 != -1 && (Conversions[num] & (1 << num2)) != 0)
		{
			try
			{
				result = Convert.ChangeType(value, type, culture);
				return true;
			}
			catch
			{
				result = null;
				return false;
			}
		}
		TypeConverter converter = TypeDescriptor.GetConverter(type);
		if (converter.CanConvertFrom(type2))
		{
			result = converter.ConvertFrom(null, culture, value);
			return true;
		}
		TypeConverter converter2 = TypeDescriptor.GetConverter(type2);
		if (converter2.CanConvertTo(type))
		{
			result = converter2.ConvertTo(null, culture, value, type);
			return true;
		}
		MethodInfo methodInfo = FindTypeConversionOperatorMethod(type2, type, OperatorType.Implicit | OperatorType.Explicit);
		if (methodInfo != null)
		{
			result = methodInfo.Invoke(null, new object[1] { value });
			return true;
		}
		result = null;
		return false;
	}

	/// <summary>
	/// Try to convert a value to a type using the implicit conversions allowed by the C#
	/// language.
	/// </summary>
	/// <param name="to">The type to convert to.</param>
	/// <param name="value">The value to convert.</param>
	/// <param name="result">If successful, contains the converted value.</param>
	/// <returns>True if the convert was successful, otherwise false.</returns>
	[RequiresUnreferencedCode("Implicit conversion methods are required for type conversion.")]
	public static bool TryConvertImplicit(Type to, object? value, out object? result)
	{
		if (value == null)
		{
			result = null;
			return AcceptsNull(to);
		}
		if (value == AvaloniaProperty.UnsetValue)
		{
			result = value;
			return true;
		}
		Type type = value.GetType();
		if (to.IsAssignableFrom(type))
		{
			result = value;
			return true;
		}
		int num = Array.IndexOf(InbuiltTypes, type);
		int num2 = Array.IndexOf(InbuiltTypes, to);
		if (num != -1 && num2 != -1 && (ImplicitConversions[num] & (1 << num2)) != 0)
		{
			try
			{
				result = Convert.ChangeType(value, to, CultureInfo.InvariantCulture);
				return true;
			}
			catch
			{
				result = null;
				return false;
			}
		}
		MethodInfo methodInfo = FindTypeConversionOperatorMethod(type, to, OperatorType.Implicit);
		if (methodInfo != null)
		{
			result = methodInfo.Invoke(null, new object[1] { value });
			return true;
		}
		result = null;
		return false;
	}

	/// <summary>
	/// Convert a value to a type by any means possible, returning the default for that type
	/// if the value could not be converted.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="type">The type to convert to.</param>
	/// <param name="culture">The culture to use.</param>
	/// <returns>A value of <paramref name="type" />.</returns>
	[RequiresUnreferencedCode("Conversion methods are required for type conversion, including op_Implicit, op_Explicit, Parse and TypeConverter.")]
	public static object? ConvertOrDefault(object? value, Type type, CultureInfo culture)
	{
		if (!TryConvert(type, value, culture, out object result))
		{
			return Default(type);
		}
		return result;
	}

	/// <summary>
	/// Convert a value to a type using the implicit conversions allowed by the C# language or
	/// return the default for the type if the value could not be converted.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="type">The type to convert to.</param>
	/// <returns>A value of <paramref name="type" />.</returns>
	[RequiresUnreferencedCode("Implicit conversion methods are required for type conversion.")]
	public static object? ConvertImplicitOrDefault(object? value, Type type)
	{
		if (!TryConvertImplicit(type, value, out object result))
		{
			return Default(type);
		}
		return result;
	}

	[RequiresUnreferencedCode("Implicit conversion methods are required for type conversion.")]
	public static T ConvertImplicit<T>(object? value)
	{
		if (TryConvertImplicit(typeof(T), value, out object result))
		{
			return (T)result;
		}
		throw new InvalidCastException($"Unable to convert object '{value ?? "(null)"}' of type '{value?.GetType()}' to type '{typeof(T)}'.");
	}

	/// <summary>
	/// Gets the default value for the specified type.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <returns>The default value.</returns>
	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "We don't care about public ctors for the value types, and always return null for the ref types.")]
	public static object? Default(Type type)
	{
		if (type.IsValueType)
		{
			return Activator.CreateInstance(type);
		}
		return null;
	}

	/// <summary>
	/// Determines if a type is numeric.  Nullable numeric types are considered numeric.
	/// </summary>
	/// <returns>
	/// True if the type is numeric; otherwise false.
	/// </returns>
	/// <remarks>
	/// Boolean is not considered numeric.
	/// </remarks>
	public static bool IsNumeric(Type type)
	{
		Type underlyingType = Nullable.GetUnderlyingType(type);
		if (underlyingType != null)
		{
			return IsNumeric(underlyingType);
		}
		return NumericTypes.Contains(type, null);
	}

	private static bool IsNullableType(Type type)
	{
		if (type.IsGenericType)
		{
			return type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}
		return false;
	}

	internal static MethodInfo? FindTypeConversionOperatorMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type fromType, Type toType, OperatorType operatorType)
	{
		bool flag = operatorType.HasAllFlags(OperatorType.Implicit);
		bool flag2 = operatorType.HasAllFlags(OperatorType.Explicit);
		MethodInfo[] methods = fromType.GetMethods();
		foreach (MethodInfo methodInfo in methods)
		{
			if (methodInfo.IsSpecialName && !(methodInfo.ReturnType != toType))
			{
				if (flag && methodInfo.Name == "op_Implicit")
				{
					return methodInfo;
				}
				if (flag2 && methodInfo.Name == "op_Explicit")
				{
					return methodInfo;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Determines whether the specified object instances are "identity" equal which means
	/// reference equal for reference types and <see cref="M:System.Object.Equals(System.Object)" /> for value
	/// types.
	/// </summary>
	/// <param name="a">The first object to compare.</param>
	/// <param name="b">The second object to compare.</param>
	/// <param name="type">
	/// The type which determines whether the objects should be treated as a reference or
	/// value type.
	/// </param>
	/// <returns>True if the objects are considered equal; otherwise false.</returns>
	internal static bool IdentityEquals(object? a, object? b, Type type)
	{
		if (type.IsValueType || type == typeof(string))
		{
			return object.Equals(a, b);
		}
		return a == b;
	}
}
