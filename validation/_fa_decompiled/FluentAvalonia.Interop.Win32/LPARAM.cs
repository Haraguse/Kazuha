using System;

namespace FluentAvalonia.Interop.Win32;

internal readonly struct LPARAM(nint value) : IComparable, IEquatable<LPARAM>
{
	public readonly nint Value = value;

	public static bool operator ==(LPARAM left, LPARAM right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(LPARAM left, LPARAM right)
	{
		return left.Value != right.Value;
	}

	public static bool operator <(LPARAM left, LPARAM right)
	{
		return left.Value < right.Value;
	}

	public static bool operator <=(LPARAM left, LPARAM right)
	{
		return left.Value <= right.Value;
	}

	public static bool operator >(LPARAM left, LPARAM right)
	{
		return left.Value > right.Value;
	}

	public static bool operator >=(LPARAM left, LPARAM right)
	{
		return left.Value >= right.Value;
	}

	public static implicit operator LPARAM(byte value)
	{
		return new LPARAM(value);
	}

	public static explicit operator byte(LPARAM value)
	{
		return (byte)value.Value;
	}

	public static implicit operator LPARAM(short value)
	{
		return new LPARAM(value);
	}

	public static explicit operator short(LPARAM value)
	{
		return (short)value.Value;
	}

	public static implicit operator LPARAM(int value)
	{
		return new LPARAM(value);
	}

	public static explicit operator int(LPARAM value)
	{
		return (int)value.Value;
	}

	public static explicit operator LPARAM(long value)
	{
		return new LPARAM((nint)value);
	}

	public static implicit operator long(LPARAM value)
	{
		return value.Value;
	}

	public static implicit operator LPARAM(nint value)
	{
		return new LPARAM(value);
	}

	public static implicit operator nint(LPARAM value)
	{
		return value.Value;
	}

	public static implicit operator LPARAM(sbyte value)
	{
		return new LPARAM(value);
	}

	public static explicit operator sbyte(LPARAM value)
	{
		return (sbyte)value.Value;
	}

	public static implicit operator LPARAM(ushort value)
	{
		return new LPARAM(value);
	}

	public static explicit operator ushort(LPARAM value)
	{
		return (ushort)value.Value;
	}

	public static explicit operator LPARAM(uint value)
	{
		return new LPARAM((nint)value);
	}

	public static explicit operator uint(LPARAM value)
	{
		return (uint)value.Value;
	}

	public static explicit operator LPARAM(ulong value)
	{
		return new LPARAM((nint)value);
	}

	public static explicit operator ulong(LPARAM value)
	{
		return (ulong)value.Value;
	}

	public static explicit operator LPARAM(nuint value)
	{
		return new LPARAM((nint)value);
	}

	public static explicit operator nuint(LPARAM value)
	{
		return (nuint)value.Value;
	}

	public int CompareTo(object obj)
	{
		if (obj is LPARAM lPARAM)
		{
			return CompareTo(lPARAM);
		}
		if (obj != null)
		{
			throw new ArgumentException("obj is not an instance of LPARAM.");
		}
		return 1;
	}

	public override bool Equals(object obj)
	{
		if (obj is LPARAM other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(LPARAM other)
	{
		return ((IntPtr)Value).Equals(other.Value);
	}

	public override int GetHashCode()
	{
		return ((IntPtr)Value).GetHashCode();
	}

	public unsafe override string ToString()
	{
		return ((IntPtr)Value).ToString((sizeof(nint) == 4) ? "X8" : "X16");
	}
}
