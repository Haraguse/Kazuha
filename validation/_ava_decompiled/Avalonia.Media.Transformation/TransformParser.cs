using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Media.Transformation;

internal static class TransformParser
{
	private enum Unit
	{
		None,
		Pixel,
		Radian,
		Gradian,
		Degree,
		Turn
	}

	private readonly struct UnitValue(Unit unit, double value)
	{
		public readonly Unit Unit = unit;

		public readonly double Value = value;

		public static UnitValue Zero => new UnitValue(Unit.None, 0.0);

		public static UnitValue One => new UnitValue(Unit.None, 1.0);
	}

	private enum TransformFunction
	{
		Invalid,
		Translate,
		TranslateX,
		TranslateY,
		Scale,
		ScaleX,
		ScaleY,
		Skew,
		SkewX,
		SkewY,
		Rotate,
		Matrix
	}

	private static readonly (string, TransformFunction)[] s_functionMapping = new(string, TransformFunction)[11]
	{
		("translate", TransformFunction.Translate),
		("translateX", TransformFunction.TranslateX),
		("translateY", TransformFunction.TranslateY),
		("scale", TransformFunction.Scale),
		("scaleX", TransformFunction.ScaleX),
		("scaleY", TransformFunction.ScaleY),
		("skew", TransformFunction.Skew),
		("skewX", TransformFunction.SkewX),
		("skewY", TransformFunction.SkewY),
		("rotate", TransformFunction.Rotate),
		("matrix", TransformFunction.Matrix)
	};

	private static readonly (string, Unit)[] s_unitMapping = new(string, Unit)[5]
	{
		("deg", Unit.Degree),
		("grad", Unit.Gradian),
		("rad", Unit.Radian),
		("turn", Unit.Turn),
		("px", Unit.Pixel)
	};

	public static TransformOperations Parse(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			throw new ArgumentException("s");
		}
		ReadOnlySpan<char> span = s.AsSpan().Trim();
		if (span.Equals("none".AsSpan(), StringComparison.OrdinalIgnoreCase))
		{
			return TransformOperations.Identity;
		}
		TransformOperations.Builder builder = TransformOperations.CreateBuilder(0);
		do
		{
			int num = span.IndexOf('(');
			int num2 = span.IndexOf(')');
			if (num == -1 || num2 == -1)
			{
				ThrowInvalidFormat();
			}
			TransformFunction transformFunction = ParseTransformFunction(span.Slice(0, num).Trim());
			if (transformFunction == TransformFunction.Invalid)
			{
				ThrowInvalidFormat();
			}
			ParseFunction(span.Slice(num + 1, num2 - num - 1).Trim(), transformFunction, in builder);
			span = span.Slice(num2 + 1);
		}
		while (!span.IsWhiteSpace());
		return builder.Build();
		void ThrowInvalidFormat()
		{
			throw new FormatException("Invalid transform string: '" + s + "'.");
		}
	}

	private static void ParseFunction(in ReadOnlySpan<char> functionPart, TransformFunction function, in TransformOperations.Builder builder)
	{
		switch (function)
		{
		case TransformFunction.Scale:
		case TransformFunction.ScaleX:
		case TransformFunction.ScaleY:
		{
			UnitValue leftValue4 = UnitValue.One;
			UnitValue rightValue4 = UnitValue.One;
			int num = ParseValuePair(in functionPart, ref leftValue4, ref rightValue4);
			if (num != 1 && (function == TransformFunction.ScaleX || function == TransformFunction.ScaleY))
			{
				ThrowFormatInvalidValueCount(function, 1);
			}
			VerifyZeroOrUnit(function, in leftValue4, Unit.None);
			VerifyZeroOrUnit(function, in rightValue4, Unit.None);
			switch (function)
			{
			case TransformFunction.ScaleY:
				rightValue4 = leftValue4;
				leftValue4 = UnitValue.One;
				break;
			case TransformFunction.Scale:
				if (num == 1)
				{
					rightValue4 = leftValue4;
				}
				break;
			}
			builder.AppendScale(leftValue4.Value, rightValue4.Value);
			break;
		}
		case TransformFunction.Skew:
		case TransformFunction.SkewX:
		case TransformFunction.SkewY:
		{
			UnitValue leftValue = UnitValue.Zero;
			UnitValue rightValue = UnitValue.Zero;
			if (ParseValuePair(in functionPart, ref leftValue, ref rightValue) != 1 && (function == TransformFunction.SkewX || function == TransformFunction.SkewY))
			{
				ThrowFormatInvalidValueCount(function, 1);
			}
			VerifyZeroOrAngle(function, in leftValue);
			VerifyZeroOrAngle(function, in rightValue);
			if (function == TransformFunction.SkewY)
			{
				rightValue = leftValue;
				leftValue = UnitValue.Zero;
			}
			builder.AppendSkew(ToRadians(in leftValue), ToRadians(in rightValue));
			break;
		}
		case TransformFunction.Rotate:
		{
			UnitValue leftValue3 = UnitValue.Zero;
			UnitValue rightValue3 = default(UnitValue);
			if (ParseValuePair(in functionPart, ref leftValue3, ref rightValue3) != 1)
			{
				ThrowFormatInvalidValueCount(function, 1);
			}
			VerifyZeroOrAngle(function, in leftValue3);
			builder.AppendRotate(ToRadians(in leftValue3));
			break;
		}
		case TransformFunction.Translate:
		case TransformFunction.TranslateX:
		case TransformFunction.TranslateY:
		{
			UnitValue leftValue2 = UnitValue.Zero;
			UnitValue rightValue2 = UnitValue.Zero;
			if (ParseValuePair(in functionPart, ref leftValue2, ref rightValue2) != 1 && (function == TransformFunction.TranslateX || function == TransformFunction.TranslateY))
			{
				ThrowFormatInvalidValueCount(function, 1);
			}
			VerifyZeroOrUnit(function, in leftValue2, Unit.Pixel);
			VerifyZeroOrUnit(function, in rightValue2, Unit.Pixel);
			if (function == TransformFunction.TranslateY)
			{
				rightValue2 = leftValue2;
				leftValue2 = UnitValue.Zero;
			}
			builder.AppendTranslate(leftValue2.Value, rightValue2.Value);
			break;
		}
		case TransformFunction.Matrix:
		{
			Span<UnitValue> outValues = stackalloc UnitValue[6];
			if (ParseCommaDelimitedValues(functionPart, in outValues) != 6)
			{
				ThrowFormatInvalidValueCount(function, 6);
			}
			Span<UnitValue> span = outValues;
			for (int i = 0; i < span.Length; i++)
			{
				UnitValue value = span[i];
				VerifyZeroOrUnit(function, in value, Unit.None);
			}
			Matrix matrix = new Matrix(outValues[0].Value, outValues[1].Value, outValues[2].Value, outValues[3].Value, outValues[4].Value, outValues[5].Value);
			builder.AppendMatrix(matrix);
			break;
		}
		}
		static int ParseCommaDelimitedValues(ReadOnlySpan<char> part, in Span<UnitValue> reference)
		{
			int num2 = 0;
			while (true)
			{
				if (num2 >= reference.Length)
				{
					throw new FormatException("Too many provided values.");
				}
				int num3 = part.IndexOf(',');
				if (num3 == -1)
				{
					break;
				}
				ReadOnlySpan<char> part2 = part.Slice(0, num3).Trim();
				reference[num2++] = ParseValue(part2);
				part = part.Slice(num3 + 1, part.Length - num3 - 1);
			}
			if (!part.IsWhiteSpace())
			{
				reference[num2++] = ParseValue(part);
			}
			return num2;
		}
		static UnitValue ParseValue(ReadOnlySpan<char> part)
		{
			int num2 = -1;
			for (int j = 0; j < part.Length; j++)
			{
				char c = part[j];
				if (!char.IsDigit(c) && c != '-' && c != '.')
				{
					num2 = j;
					break;
				}
			}
			Unit unit = Unit.None;
			if (num2 != -1)
			{
				unit = ParseUnit(part.Slice(num2, part.Length - num2));
				part = part.Slice(0, num2);
			}
			double value2 = double.Parse(part.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
			return new UnitValue(unit, value2);
		}
		static int ParseValuePair(in ReadOnlySpan<char> part, ref UnitValue reference, ref UnitValue reference2)
		{
			int num2 = part.IndexOf(',');
			if (num2 != -1)
			{
				ReadOnlySpan<char> part2 = part.Slice(0, num2).Trim();
				ReadOnlySpan<char> part3 = part.Slice(num2 + 1, part.Length - num2 - 1).Trim();
				reference = ParseValue(part2);
				reference2 = ParseValue(part3);
				return 2;
			}
			reference = ParseValue(part);
			return 1;
		}
	}

	private static void VerifyZeroOrUnit(TransformFunction function, in UnitValue value, Unit unit)
	{
		if ((value.Unit != Unit.None || value.Value != 0.0) && value.Unit != unit)
		{
			ThrowFormatInvalidValue(function, in value);
		}
	}

	private static void VerifyZeroOrAngle(TransformFunction function, in UnitValue value)
	{
		if (value.Value != 0.0 && !IsAngleUnit(value.Unit))
		{
			ThrowFormatInvalidValue(function, in value);
		}
	}

	private static bool IsAngleUnit(Unit unit)
	{
		if ((uint)(unit - 2) <= 3u)
		{
			return true;
		}
		return false;
	}

	private static void ThrowFormatInvalidValue(TransformFunction function, in UnitValue value)
	{
		string value2 = ((value.Unit == Unit.None) ? string.Empty : value.Unit.ToString());
		throw new FormatException($"Invalid value {value.Value} {value2} for {function}");
	}

	private static void ThrowFormatInvalidValueCount(TransformFunction function, int count)
	{
		throw new FormatException($"Invalid format. {function} expects {count} value(s).");
	}

	private static Unit ParseUnit(in ReadOnlySpan<char> part)
	{
		(string, Unit)[] array = s_unitMapping;
		for (int i = 0; i < array.Length; i++)
		{
			var (text, result) = array[i];
			if (part.Equals(text.AsSpan(), StringComparison.OrdinalIgnoreCase))
			{
				return result;
			}
		}
		throw new FormatException("Invalid unit: " + part.ToString());
	}

	private static TransformFunction ParseTransformFunction(in ReadOnlySpan<char> part)
	{
		(string, TransformFunction)[] array = s_functionMapping;
		for (int i = 0; i < array.Length; i++)
		{
			var (text, result) = array[i];
			if (part.Equals(text.AsSpan(), StringComparison.OrdinalIgnoreCase))
			{
				return result;
			}
		}
		return TransformFunction.Invalid;
	}

	private static double ToRadians(in UnitValue value)
	{
		return value.Unit switch
		{
			Unit.Radian => value.Value, 
			Unit.Gradian => MathUtilities.Grad2Rad(value.Value), 
			Unit.Degree => MathUtilities.Deg2Rad(value.Value), 
			Unit.Turn => MathUtilities.Turn2Rad(value.Value), 
			_ => value.Value, 
		};
	}
}
