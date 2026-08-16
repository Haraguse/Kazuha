using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Windows.Input;
using Avalonia.Data.Converters;
using Avalonia.Utilities;

namespace Avalonia.Data.Core;

internal abstract class TargetTypeConverter
{
	private class DefaultConverter : TargetTypeConverter
	{
		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.")]
		[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.")]
		[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Conversion methods might be removed by the linker. We don't have a reliable way to prevent it, except converting everything in compile time when possible.")]
		public override bool TryConvert(object? value, Type type, CultureInfo culture, out object? result)
		{
			if (value?.GetType() == type)
			{
				result = value;
				return true;
			}
			Type type2 = Nullable.GetUnderlyingType(type) ?? type;
			if (value == null)
			{
				result = null;
				if (type2.IsValueType)
				{
					return type2 != type;
				}
				return true;
			}
			if (value == AvaloniaProperty.UnsetValue)
			{
				result = null;
				return false;
			}
			if (type2.IsAssignableFrom(value.GetType()))
			{
				result = value;
				return true;
			}
			if (type2 == typeof(string))
			{
				result = value.ToString();
				return true;
			}
			if (type2.IsEnum && type2.GetEnumUnderlyingType() == value.GetType())
			{
				result = Enum.ToObject(type2, value);
				return true;
			}
			TypeConverter converter = TypeDescriptor.GetConverter(type2);
			Type type3 = value.GetType();
			if (converter.CanConvertFrom(type3))
			{
				try
				{
					result = converter.ConvertFrom(null, culture, value);
					return true;
				}
				catch
				{
					result = null;
					return false;
				}
			}
			TypeConverter converter2 = TypeDescriptor.GetConverter(type3);
			if (converter2.CanConvertTo(type2))
			{
				try
				{
					result = converter2.ConvertTo(null, culture, value, type2);
					return true;
				}
				catch
				{
					result = null;
					return false;
				}
			}
			MethodInfo methodInfo = TypeUtilities.FindTypeConversionOperatorMethod(value.GetType(), type2, TypeUtilities.OperatorType.Implicit | TypeUtilities.OperatorType.Explicit);
			if ((object)methodInfo != null)
			{
				try
				{
					result = methodInfo.Invoke(null, new object[1] { value });
					return true;
				}
				catch
				{
					result = null;
					return false;
				}
			}
			if (value is IConvertible convertible)
			{
				try
				{
					result = convertible.ToType(type2, culture);
					return true;
				}
				catch
				{
					result = null;
					return false;
				}
			}
			result = null;
			return false;
		}
	}

	[RequiresUnreferencedCode("Conversion methods are required for type conversion, including op_Implicit, op_Explicit, Parse and TypeConverter.")]
	private class ReflectionConverter : TargetTypeConverter
	{
		public override bool TryConvert(object? value, Type type, CultureInfo culture, out object? result)
		{
			if (value?.GetType() == type)
			{
				result = value;
				return true;
			}
			if (value == AvaloniaProperty.UnsetValue)
			{
				result = Activator.CreateInstance(type);
				return true;
			}
			if (typeof(ICommand).IsAssignableFrom(type) && value is Delegate obj && !obj.Method.IsPrivate && obj.Method.GetParameters().Length <= 1)
			{
				result = new MethodToCommandConverter(obj);
				return true;
			}
			return TypeUtilities.TryConvert(type, value, culture, out result);
		}
	}

	private static TargetTypeConverter? s_default;

	private static TargetTypeConverter? s_reflection;

	public static TargetTypeConverter GetDefaultConverter()
	{
		return s_default ?? (s_default = new DefaultConverter());
	}

	[RequiresUnreferencedCode("Conversion methods are required for type conversion, including op_Implicit, op_Explicit, Parse and TypeConverter.")]
	public static TargetTypeConverter GetReflectionConverter()
	{
		return s_reflection ?? (s_reflection = new ReflectionConverter());
	}

	public abstract bool TryConvert(object? value, Type type, CultureInfo culture, out object? result);
}
