using System;

namespace FluentAvalonia.Interop.Win32;

internal readonly struct LRESULT(nint value) : IComparable, IEquatable<LRESULT>
{
	public readonly nint Value = value;

	public static bool operator ==(LRESULT left, LRESULT right)
	{
		return left.Value == right.Value;
	}

	public static bool operator !=(LRESULT left, LRESULT right)
	{
		return left.Value != right.Value;
	}

	public static bool operator <(LRESULT left, LRESULT right)
	{
		return left.Value < right.Value;
	}

	public static bool operator <=(LRESULT left, LRESULT right)
	{
		return left.Value <= right.Value;
	}

	public static bool operator >(LRESULT left, LRESULT right)
	{
		return left.Value > right.Value;
	}

	public static bool operator >=(LRESULT left, LRESULT right)
	{
		return left.Value >= right.Value;
	}

	public static implicit operator LRESULT(byte value)
	{
		return new LRESULT(value);
	}

	public static explicit operator byte(LRESULT value)
	{
		return (byte)value.Value;
	}

	public static implicit operator LRESULT(short value)
	{
		return new LRESULT(value);
	}

	public static explicit operator short(LRESULT value)
	{
		return (short)value.Value;
	}

	public static implicit operator LRESULT(int value)
	{
		return new LRESULT(value);
	}

	public static explicit operator int(LRESULT value)
	{
		return (int)value.Value;
	}

	public static explicit operator LRESULT(long value)
	{
		return new LRESULT((nint)value);
	}

	public static implicit operator long(LRESULT value)
	{
		return value.Value;
	}

	public static implicit operator LRESULT(nint value)
	{
		return new LRESULT(value);
	}

	public static implicit operator nint(LRESULT value)
	{
		return value.Value;
	}

	public static implicit operator LRESULT(sbyte value)
	{
		return new LRESULT(value);
	}

	public static explicit operator sbyte(LRESULT value)
	{
		return (sbyte)value.Value;
	}

	public static implicit operator LRESULT(ushort value)
	{
		return new LRESULT(value);
	}

	public static explicit operator ushort(LRESULT value)
	{
		return (ushort)value.Value;
	}

	public static explicit operator LRESULT(uint value)
	{
		return new LRESULT((nint)value);
	}

	public static explicit operator uint(LRESULT value)
	{
		return (uint)value.Value;
	}

	public static explicit operator LRESULT(ulong value)
	{
		return new LRESULT((nint)value);
	}

	public static explicit operator ulong(LRESULT value)
	{
		return (ulong)value.Value;
	}

	public static explicit operator LRESULT(nuint value)
	{
		return new LRESULT((nint)value);
	}

	public static explicit operator nuint(LRESULT value)
	{
		return (nuint)value.Value;
	}

	public int CompareTo(object obj)
	{
		if (obj is LRESULT lRESULT)
		{
			return CompareTo(lRESULT);
		}
		if (obj != null)
		{
			throw new ArgumentException("obj is not an instance of LRESULT.");
		}
		return 1;
	}

	public override bool Equals(object obj)
	{
		if (obj is LRESULT other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(LRESULT other)
	{
		return ((IntPtr)Value).Equals(other.Value);
	}

	public override int GetHashCode()
	{
		return ((IntPtr)Value).GetHashCode();
	}

	public override string ToString()
	{
		return ((IntPtr)Value).ToString();
	}

	public unsafe static explicit operator LRESULT(void* value)
	{
		return new LRESULT((nint)value);
	}

	public unsafe static implicit operator void*(LRESULT value)
	{
		return (void*)value.Value;
	}

	public static explicit operator LRESULT(BOOL value)
	{
		return new LRESULT(value.Value);
	}

	public static explicit operator BOOL(LRESULT value)
	{
		return new BOOL((int)value.Value);
	}

	public unsafe static explicit operator LRESULT(HMENU value)
	{
		return new LRESULT((nint)value.Value);
	}

	public unsafe static explicit operator HMENU(LRESULT value)
	{
		return new HMENU((void*)value.Value);
	}

	public unsafe static explicit operator LRESULT(HWND value)
	{
		return new LRESULT((nint)value.Value);
	}

	public unsafe static explicit operator HWND(LRESULT value)
	{
		return new HWND((void*)value.Value);
	}

	public static explicit operator LRESULT(LPARAM value)
	{
		return new LRESULT(value.Value);
	}

	public static explicit operator LRESULT(WPARAM value)
	{
		return new LRESULT((nint)value.Value);
	}
}
