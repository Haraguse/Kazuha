using System;

namespace FluentAvalonia.Interop.Win32;

internal readonly struct BOOL(int value) : IComparable, IComparable<BOOL>, IEquatable<BOOL>, IFormattable
{
	public readonly int Value = value;

	public static BOOL FALSE => new BOOL(0);

	public static BOOL TRUE => new BOOL(1);

	public static bool operator ==(BOOL left, BOOL right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(BOOL left, BOOL right)
	{
		return left.Value != right.Value;
	}

	public static bool operator <(BOOL left, BOOL right)
	{
		return left.Value < right.Value;
	}

	public static bool operator <=(BOOL left, BOOL right)
	{
		return left.Value <= right.Value;
	}

	public static bool operator >(BOOL left, BOOL right)
	{
		return left.Value > right.Value;
	}

	public static bool operator >=(BOOL left, BOOL right)
	{
		return left.Value >= right.Value;
	}

	public static implicit operator bool(BOOL value)
	{
		return value.Value != 0;
	}

	public static implicit operator BOOL(bool value)
	{
		return new BOOL(value ? 1 : 0);
	}

	public static bool operator false(BOOL value)
	{
		return value.Value == 0;
	}

	public static bool operator true(BOOL value)
	{
		return value.Value != 0;
	}

	public static implicit operator BOOL(byte value)
	{
		return new BOOL(value);
	}

	public static explicit operator byte(BOOL value)
	{
		return (byte)value.Value;
	}

	public static implicit operator BOOL(short value)
	{
		return new BOOL(value);
	}

	public static explicit operator short(BOOL value)
	{
		return (short)value.Value;
	}

	public static implicit operator BOOL(int value)
	{
		return new BOOL(value);
	}

	public static implicit operator int(BOOL value)
	{
		return value.Value;
	}

	public static explicit operator BOOL(long value)
	{
		return new BOOL((int)value);
	}

	public static implicit operator long(BOOL value)
	{
		return value.Value;
	}

	public static explicit operator BOOL(nint value)
	{
		return new BOOL((int)value);
	}

	public static implicit operator nint(BOOL value)
	{
		return value.Value;
	}

	public static implicit operator BOOL(sbyte value)
	{
		return new BOOL(value);
	}

	public static explicit operator sbyte(BOOL value)
	{
		return (sbyte)value.Value;
	}

	public static implicit operator BOOL(ushort value)
	{
		return new BOOL(value);
	}

	public static explicit operator ushort(BOOL value)
	{
		return (ushort)value.Value;
	}

	public static explicit operator BOOL(uint value)
	{
		return new BOOL((int)value);
	}

	public static explicit operator uint(BOOL value)
	{
		return (uint)value.Value;
	}

	public static explicit operator BOOL(ulong value)
	{
		return new BOOL((int)value);
	}

	public static explicit operator ulong(BOOL value)
	{
		return (ulong)value.Value;
	}

	public static explicit operator BOOL(nuint value)
	{
		return new BOOL((int)value);
	}

	public static explicit operator nuint(BOOL value)
	{
		return (nuint)value.Value;
	}

	public int CompareTo(object obj)
	{
		if (obj is BOOL other)
		{
			return CompareTo(other);
		}
		if (obj != null)
		{
			throw new ArgumentException("obj is not an instance of BOOL.");
		}
		return 1;
	}

	public int CompareTo(BOOL other)
	{
		return Value.CompareTo(other.Value);
	}

	public override bool Equals(object obj)
	{
		if (obj is BOOL other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(BOOL other)
	{
		return Value.Equals(other.Value);
	}

	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public override string ToString()
	{
		return Value.ToString();
	}

	public string ToString(string format, IFormatProvider formatProvider)
	{
		return Value.ToString(format, formatProvider);
	}
}
