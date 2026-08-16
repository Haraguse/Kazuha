using System;

namespace FluentAvalonia.Interop.Win32;

internal readonly struct HRESULT(int value) : IComparable, IComparable<HRESULT>, IEquatable<HRESULT>, IFormattable
{
	public readonly int Value = value;

	public bool FAILED => Value < 0;

	public bool SUCCEEDED => Value >= 0;

	public static bool operator ==(HRESULT left, HRESULT right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(HRESULT left, HRESULT right)
	{
		return left.Value != right.Value;
	}

	public static bool operator <(HRESULT left, HRESULT right)
	{
		return left.Value < right.Value;
	}

	public static bool operator <=(HRESULT left, HRESULT right)
	{
		return left.Value <= right.Value;
	}

	public static bool operator >(HRESULT left, HRESULT right)
	{
		return left.Value > right.Value;
	}

	public static bool operator >=(HRESULT left, HRESULT right)
	{
		return left.Value >= right.Value;
	}

	public static implicit operator HRESULT(byte value)
	{
		return new HRESULT(value);
	}

	public static explicit operator byte(HRESULT value)
	{
		return (byte)value.Value;
	}

	public static implicit operator HRESULT(short value)
	{
		return new HRESULT(value);
	}

	public static explicit operator short(HRESULT value)
	{
		return (short)value.Value;
	}

	public static implicit operator HRESULT(int value)
	{
		return new HRESULT(value);
	}

	public static implicit operator int(HRESULT value)
	{
		return value.Value;
	}

	public static explicit operator HRESULT(long value)
	{
		return new HRESULT((int)value);
	}

	public static implicit operator long(HRESULT value)
	{
		return value.Value;
	}

	public static explicit operator HRESULT(nint value)
	{
		return new HRESULT((int)value);
	}

	public static implicit operator nint(HRESULT value)
	{
		return value.Value;
	}

	public static implicit operator HRESULT(sbyte value)
	{
		return new HRESULT(value);
	}

	public static explicit operator sbyte(HRESULT value)
	{
		return (sbyte)value.Value;
	}

	public static implicit operator HRESULT(ushort value)
	{
		return new HRESULT(value);
	}

	public static explicit operator ushort(HRESULT value)
	{
		return (ushort)value.Value;
	}

	public static explicit operator HRESULT(uint value)
	{
		return new HRESULT((int)value);
	}

	public static explicit operator uint(HRESULT value)
	{
		return (uint)value.Value;
	}

	public static explicit operator HRESULT(ulong value)
	{
		return new HRESULT((int)value);
	}

	public static explicit operator ulong(HRESULT value)
	{
		return (ulong)value.Value;
	}

	public static explicit operator HRESULT(nuint value)
	{
		return new HRESULT((int)value);
	}

	public static explicit operator nuint(HRESULT value)
	{
		return (nuint)value.Value;
	}

	public int CompareTo(object obj)
	{
		if (obj is HRESULT other)
		{
			return CompareTo(other);
		}
		if (obj != null)
		{
			throw new ArgumentException("obj is not an instance of HRESULT.");
		}
		return 1;
	}

	public int CompareTo(HRESULT other)
	{
		return Value.CompareTo(other.Value);
	}

	public override bool Equals(object obj)
	{
		if (obj is HRESULT other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(HRESULT other)
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
