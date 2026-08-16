using System;

namespace FluentAvalonia.Interop.Win32;

internal unsafe readonly struct HWND(void* value) : IComparable, IEquatable<HWND>
{
	public unsafe readonly void* Value = value;

	public unsafe static HWND INVALID_VALUE => new HWND((void*)(-1));

	public unsafe static HWND NULL => new HWND(null);

	public unsafe static bool operator ==(HWND left, HWND right)
	{
		return left.Value == right.Value;
	}

	public unsafe static bool operator !=(HWND left, HWND right)
	{
		return left.Value != right.Value;
	}

	public unsafe static bool operator <(HWND left, HWND right)
	{
		return left.Value < right.Value;
	}

	public unsafe static bool operator <=(HWND left, HWND right)
	{
		return left.Value <= right.Value;
	}

	public unsafe static bool operator >(HWND left, HWND right)
	{
		return left.Value > right.Value;
	}

	public unsafe static bool operator >=(HWND left, HWND right)
	{
		return left.Value >= right.Value;
	}

	public unsafe static explicit operator HWND(void* value)
	{
		return new HWND(value);
	}

	public unsafe static implicit operator void*(HWND value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HWND(byte value)
	{
		return new HWND((void*)value);
	}

	public unsafe static explicit operator byte(HWND value)
	{
		return (byte)value.Value;
	}

	public unsafe static explicit operator HWND(short value)
	{
		return new HWND((void*)value);
	}

	public unsafe static explicit operator short(HWND value)
	{
		return (short)value.Value;
	}

	public unsafe static explicit operator HWND(int value)
	{
		return new HWND((void*)value);
	}

	public unsafe static explicit operator int(HWND value)
	{
		return (int)value.Value;
	}

	public unsafe static explicit operator HWND(long value)
	{
		return new HWND((void*)value);
	}

	public unsafe static explicit operator long(HWND value)
	{
		return (long)value.Value;
	}

	public unsafe static explicit operator HWND(nint value)
	{
		return new HWND((void*)value);
	}

	public unsafe static implicit operator nint(HWND value)
	{
		return (nint)value.Value;
	}

	public unsafe static explicit operator HWND(sbyte value)
	{
		return new HWND((void*)value);
	}

	public unsafe static explicit operator sbyte(HWND value)
	{
		return (sbyte)value.Value;
	}

	public unsafe static explicit operator HWND(ushort value)
	{
		return new HWND((void*)value);
	}

	public unsafe static explicit operator ushort(HWND value)
	{
		return (ushort)value.Value;
	}

	public unsafe static explicit operator HWND(uint value)
	{
		return new HWND((void*)value);
	}

	public unsafe static explicit operator uint(HWND value)
	{
		return (uint)value.Value;
	}

	public unsafe static explicit operator HWND(ulong value)
	{
		return new HWND((void*)value);
	}

	public unsafe static explicit operator ulong(HWND value)
	{
		return (ulong)value.Value;
	}

	public unsafe static explicit operator HWND(nuint value)
	{
		return new HWND((void*)value);
	}

	public unsafe static implicit operator nuint(HWND value)
	{
		return (nuint)value.Value;
	}

	public int CompareTo(object obj)
	{
		if (obj is HWND hWND)
		{
			return CompareTo(hWND);
		}
		if (obj != null)
		{
			throw new ArgumentException("obj is not an instance of HWND.");
		}
		return 1;
	}

	public override bool Equals(object obj)
	{
		if (obj is HWND other)
		{
			return Equals(other);
		}
		return false;
	}

	public unsafe bool Equals(HWND other)
	{
		nuint value = (nuint)Value;
		return ((UIntPtr)value).Equals((nuint)other.Value);
	}

	public unsafe override int GetHashCode()
	{
		nuint value = (nuint)Value;
		return ((UIntPtr)value).GetHashCode();
	}
}
