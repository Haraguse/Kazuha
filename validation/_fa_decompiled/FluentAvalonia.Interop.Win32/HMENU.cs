using System;

namespace FluentAvalonia.Interop.Win32;

internal unsafe readonly struct HMENU(void* value) : IComparable, IEquatable<HMENU>
{
	public unsafe readonly void* Value = value;

	public unsafe static HMENU INVALID_VALUE => new HMENU((void*)(-1));

	public unsafe static HMENU NULL => new HMENU(null);

	public unsafe static bool operator ==(HMENU left, HMENU right)
	{
		return left.Value == right.Value;
	}

	public unsafe static bool operator !=(HMENU left, HMENU right)
	{
		return left.Value != right.Value;
	}

	public unsafe static bool operator <(HMENU left, HMENU right)
	{
		return left.Value < right.Value;
	}

	public unsafe static bool operator <=(HMENU left, HMENU right)
	{
		return left.Value <= right.Value;
	}

	public unsafe static bool operator >(HMENU left, HMENU right)
	{
		return left.Value > right.Value;
	}

	public unsafe static bool operator >=(HMENU left, HMENU right)
	{
		return left.Value >= right.Value;
	}

	public unsafe static explicit operator HMENU(void* value)
	{
		return new HMENU(value);
	}

	public unsafe static implicit operator void*(HMENU value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HMENU(byte value)
	{
		return new HMENU((void*)value);
	}

	public unsafe static explicit operator byte(HMENU value)
	{
		return (byte)value.Value;
	}

	public unsafe static explicit operator HMENU(short value)
	{
		return new HMENU((void*)value);
	}

	public unsafe static explicit operator short(HMENU value)
	{
		return (short)value.Value;
	}

	public unsafe static explicit operator HMENU(int value)
	{
		return new HMENU((void*)value);
	}

	public unsafe static explicit operator int(HMENU value)
	{
		return (int)value.Value;
	}

	public unsafe static explicit operator HMENU(long value)
	{
		return new HMENU((void*)value);
	}

	public unsafe static explicit operator long(HMENU value)
	{
		return (long)value.Value;
	}

	public unsafe static explicit operator HMENU(nint value)
	{
		return new HMENU((void*)value);
	}

	public unsafe static implicit operator nint(HMENU value)
	{
		return (nint)value.Value;
	}

	public unsafe static explicit operator HMENU(sbyte value)
	{
		return new HMENU((void*)value);
	}

	public unsafe static explicit operator sbyte(HMENU value)
	{
		return (sbyte)value.Value;
	}

	public unsafe static explicit operator HMENU(ushort value)
	{
		return new HMENU((void*)value);
	}

	public unsafe static explicit operator ushort(HMENU value)
	{
		return (ushort)value.Value;
	}

	public unsafe static explicit operator HMENU(uint value)
	{
		return new HMENU((void*)value);
	}

	public unsafe static explicit operator uint(HMENU value)
	{
		return (uint)value.Value;
	}

	public unsafe static explicit operator HMENU(ulong value)
	{
		return new HMENU((void*)value);
	}

	public unsafe static explicit operator ulong(HMENU value)
	{
		return (ulong)value.Value;
	}

	public unsafe static explicit operator HMENU(nuint value)
	{
		return new HMENU((void*)value);
	}

	public unsafe static implicit operator nuint(HMENU value)
	{
		return (nuint)value.Value;
	}

	public int CompareTo(object obj)
	{
		if (obj is HMENU hMENU)
		{
			return CompareTo(hMENU);
		}
		if (obj != null)
		{
			throw new ArgumentException("obj is not an instance of HMENU.");
		}
		return 1;
	}

	public override bool Equals(object obj)
	{
		if (obj is HMENU other)
		{
			return Equals(other);
		}
		return false;
	}

	public unsafe bool Equals(HMENU other)
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
