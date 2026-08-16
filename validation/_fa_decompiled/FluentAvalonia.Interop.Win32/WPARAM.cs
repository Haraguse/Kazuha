using System;

namespace FluentAvalonia.Interop.Win32;

internal readonly struct WPARAM(nuint value) : IComparable, IEquatable<WPARAM>
{
	public readonly nuint Value = value;

	public static bool operator ==(WPARAM left, WPARAM right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(WPARAM left, WPARAM right)
	{
		return left.Value != right.Value;
	}

	public static bool operator <(WPARAM left, WPARAM right)
	{
		return left.Value < right.Value;
	}

	public static bool operator <=(WPARAM left, WPARAM right)
	{
		return left.Value <= right.Value;
	}

	public static bool operator >(WPARAM left, WPARAM right)
	{
		return left.Value > right.Value;
	}

	public static bool operator >=(WPARAM left, WPARAM right)
	{
		return left.Value >= right.Value;
	}

	public static implicit operator WPARAM(byte value)
	{
		return new WPARAM(value);
	}

	public static explicit operator byte(WPARAM value)
	{
		return (byte)value.Value;
	}

	public static explicit operator WPARAM(short value)
	{
		return new WPARAM((nuint)value);
	}

	public static explicit operator short(WPARAM value)
	{
		return (short)value.Value;
	}

	public static explicit operator WPARAM(int value)
	{
		return new WPARAM((nuint)value);
	}

	public static explicit operator int(WPARAM value)
	{
		return (int)value.Value;
	}

	public static explicit operator WPARAM(long value)
	{
		return new WPARAM((nuint)value);
	}

	public static explicit operator long(WPARAM value)
	{
		return (long)value.Value;
	}

	public static explicit operator WPARAM(nint value)
	{
		return new WPARAM((nuint)value);
	}

	public static explicit operator nint(WPARAM value)
	{
		return (nint)value.Value;
	}

	public static explicit operator WPARAM(sbyte value)
	{
		return new WPARAM((nuint)value);
	}

	public static explicit operator sbyte(WPARAM value)
	{
		return (sbyte)value.Value;
	}

	public static implicit operator WPARAM(ushort value)
	{
		return new WPARAM(value);
	}

	public static explicit operator ushort(WPARAM value)
	{
		return (ushort)value.Value;
	}

	public static implicit operator WPARAM(uint value)
	{
		return new WPARAM(value);
	}

	public static explicit operator uint(WPARAM value)
	{
		return (uint)value.Value;
	}

	public static explicit operator WPARAM(ulong value)
	{
		return new WPARAM((nuint)value);
	}

	public static implicit operator ulong(WPARAM value)
	{
		return value.Value;
	}

	public static implicit operator WPARAM(nuint value)
	{
		return new WPARAM(value);
	}

	public static implicit operator nuint(WPARAM value)
	{
		return value.Value;
	}

	public int CompareTo(object obj)
	{
		if (obj is WPARAM wPARAM)
		{
			return CompareTo(wPARAM);
		}
		if (obj != null)
		{
			throw new ArgumentException("obj is not an instance of WPARAM.");
		}
		return 1;
	}

	public override bool Equals(object obj)
	{
		if (obj is WPARAM other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(WPARAM other)
	{
		return ((UIntPtr)Value).Equals(other.Value);
	}

	public override int GetHashCode()
	{
		return ((UIntPtr)Value).GetHashCode();
	}

	public unsafe static explicit operator WPARAM(void* value)
	{
		return new WPARAM((nuint)value);
	}

	public unsafe static implicit operator void*(WPARAM value)
	{
		return (void*)value.Value;
	}

	public static explicit operator WPARAM(BOOL value)
	{
		return new WPARAM((nuint)value.Value);
	}

	public static explicit operator BOOL(WPARAM value)
	{
		return new BOOL((int)value.Value);
	}

	public unsafe static explicit operator WPARAM(HMENU value)
	{
		return new WPARAM((nuint)value.Value);
	}

	public unsafe static explicit operator HMENU(WPARAM value)
	{
		return new HMENU((void*)value.Value);
	}

	public unsafe static explicit operator WPARAM(HWND value)
	{
		return new WPARAM((nuint)value.Value);
	}

	public unsafe static explicit operator HWND(WPARAM value)
	{
		return new HWND((void*)value.Value);
	}

	public static explicit operator WPARAM(LPARAM value)
	{
		return new WPARAM((nuint)value.Value);
	}

	public static explicit operator WPARAM(LRESULT value)
	{
		return new WPARAM((nuint)value.Value);
	}
}
