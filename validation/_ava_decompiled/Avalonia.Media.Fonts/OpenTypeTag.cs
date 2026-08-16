using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.Fonts;

public readonly record struct OpenTypeTag
{
	internal static readonly OpenTypeTag None = new OpenTypeTag(0, 0, 0, 0);

	internal static readonly OpenTypeTag Max = new OpenTypeTag(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	internal static readonly OpenTypeTag MaxSigned = new OpenTypeTag(127, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private readonly uint _value;

	public OpenTypeTag(uint value)
	{
		_value = value;
	}

	public OpenTypeTag(char c1, char c2, char c3, char c4)
	{
		_value = (uint)(((byte)c1 << 24) | ((byte)c2 << 16) | ((byte)c3 << 8) | (byte)c4);
	}

	private OpenTypeTag(byte c1, byte c2, byte c3, byte c4)
	{
		_value = (uint)((c1 << 24) | (c2 << 16) | (c3 << 8) | c4);
	}

	public static OpenTypeTag Parse(string tag)
	{
		if (string.IsNullOrEmpty(tag))
		{
			return None;
		}
		char[] array = new char[4];
		int num = Math.Min(4, tag.Length);
		int i;
		for (i = 0; i < num; i++)
		{
			array[i] = tag[i];
		}
		for (; i < 4; i++)
		{
			array[i] = ' ';
		}
		return new OpenTypeTag(array[0], array[1], array[2], array[3]);
	}

	public override string ToString()
	{
		if (_value == None)
		{
			return "None";
		}
		if (_value == Max)
		{
			return "Max";
		}
		if (_value == MaxSigned)
		{
			return "MaxSigned";
		}
		InlineArray4<object> buffer = default(InlineArray4<object>);
		buffer[0] = (char)(byte)(_value >> 24);
		buffer[1] = (char)(byte)(_value >> 16);
		buffer[2] = (char)(byte)(_value >> 8);
		buffer[3] = (char)(byte)_value;
		return string.Concat((ReadOnlySpan<object?>)buffer);
	}

	public static implicit operator uint(OpenTypeTag tag)
	{
		return tag._value;
	}

	public static implicit operator OpenTypeTag(uint tag)
	{
		return new OpenTypeTag(tag);
	}
}
