using System;

namespace FluentAvalonia.Interop.Win32;

internal readonly struct COLORREF(uint value) : IComparable, IComparable<COLORREF>, IEquatable<COLORREF>, IFormattable
{
	public readonly uint Value = value;

	public static bool operator ==(COLORREF left, COLORREF right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(COLORREF left, COLORREF right)
	{
		return left.Value != right.Value;
	}

	public static bool operator <(COLORREF left, COLORREF right)
	{
		return left.Value < right.Value;
	}

	public static bool operator <=(COLORREF left, COLORREF right)
	{
		return left.Value <= right.Value;
	}

	public static bool operator >(COLORREF left, COLORREF right)
	{
		return left.Value > right.Value;
	}

	public static bool operator >=(COLORREF left, COLORREF right)
	{
		return left.Value >= right.Value;
	}

	public static implicit operator COLORREF(byte value)
	{
		return new COLORREF(value);
	}

	public static explicit operator byte(COLORREF value)
	{
		return (byte)value.Value;
	}

	public static explicit operator COLORREF(short value)
	{
		return new COLORREF((uint)value);
	}

	public static explicit operator short(COLORREF value)
	{
		return (short)value.Value;
	}

	public static explicit operator COLORREF(int value)
	{
		return new COLORREF((uint)value);
	}

	public static explicit operator int(COLORREF value)
	{
		return (int)value.Value;
	}

	public static explicit operator COLORREF(long value)
	{
		return new COLORREF((uint)value);
	}

	public static implicit operator long(COLORREF value)
	{
		return value.Value;
	}

	public static explicit operator COLORREF(nint value)
	{
		return new COLORREF((uint)value);
	}

	public static explicit operator nint(COLORREF value)
	{
		return (nint)value.Value;
	}

	public static explicit operator COLORREF(sbyte value)
	{
		return new COLORREF((uint)value);
	}

	public static explicit operator sbyte(COLORREF value)
	{
		return (sbyte)value.Value;
	}

	public static implicit operator COLORREF(ushort value)
	{
		return new COLORREF(value);
	}

	public static explicit operator ushort(COLORREF value)
	{
		return (ushort)value.Value;
	}

	public static implicit operator COLORREF(uint value)
	{
		return new COLORREF(value);
	}

	public static implicit operator uint(COLORREF value)
	{
		return value.Value;
	}

	public static explicit operator COLORREF(ulong value)
	{
		return new COLORREF((uint)value);
	}

	public static implicit operator ulong(COLORREF value)
	{
		return value.Value;
	}

	public static explicit operator COLORREF(nuint value)
	{
		return new COLORREF((uint)value);
	}

	public static implicit operator nuint(COLORREF value)
	{
		return value.Value;
	}

	public int CompareTo(object obj)
	{
		if (obj is COLORREF other)
		{
			return CompareTo(other);
		}
		if (obj != null)
		{
			throw new ArgumentException("obj is not an instance of COLORREF.");
		}
		return 1;
	}

	public int CompareTo(COLORREF other)
	{
		return Value.CompareTo(other.Value);
	}

	public override bool Equals(object obj)
	{
		if (obj is COLORREF other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(COLORREF other)
	{
		return Value.Equals(other.Value);
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public override string ToString()
	{
		return Value.ToString("X8");
	}

	public string ToString(string format, IFormatProvider formatProvider)
	{
		return Value.ToString(format, formatProvider);
	}
}
