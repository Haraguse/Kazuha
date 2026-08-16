using System;

namespace FluentAvalonia.Interop.Win32;

internal unsafe readonly struct HBITMAP(void* value) : IComparable, IEquatable<HBITMAP>
{
	public unsafe readonly void* Value = value;

	public unsafe static HBITMAP INVALID_VALUE => new HBITMAP((void*)(-1));

	public unsafe static HBITMAP NULL => new HBITMAP(null);

	public unsafe static bool operator ==(HBITMAP left, HBITMAP right)
	{
		return left.Value == right.Value;
	}

	public unsafe static bool operator !=(HBITMAP left, HBITMAP right)
	{
		return left.Value != right.Value;
	}

	public unsafe static bool operator <(HBITMAP left, HBITMAP right)
	{
		return left.Value < right.Value;
	}

	public unsafe static bool operator <=(HBITMAP left, HBITMAP right)
	{
		return left.Value <= right.Value;
	}

	public unsafe static bool operator >(HBITMAP left, HBITMAP right)
	{
		return left.Value > right.Value;
	}

	public unsafe static bool operator >=(HBITMAP left, HBITMAP right)
	{
		return left.Value >= right.Value;
	}

	public unsafe static explicit operator HBITMAP(void* value)
	{
		return new HBITMAP(value);
	}

	public unsafe static implicit operator void*(HBITMAP value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HBITMAP(byte value)
	{
		return new HBITMAP((void*)value);
	}

	public unsafe static explicit operator byte(HBITMAP value)
	{
		return (byte)value.Value;
	}

	public unsafe static explicit operator HBITMAP(short value)
	{
		return new HBITMAP((void*)value);
	}

	public unsafe static explicit operator short(HBITMAP value)
	{
		return (short)value.Value;
	}

	public unsafe static explicit operator HBITMAP(int value)
	{
		return new HBITMAP((void*)value);
	}

	public unsafe static explicit operator int(HBITMAP value)
	{
		return (int)value.Value;
	}

	public unsafe static explicit operator HBITMAP(long value)
	{
		return new HBITMAP((void*)value);
	}

	public unsafe static explicit operator long(HBITMAP value)
	{
		return (long)value.Value;
	}

	public unsafe static explicit operator HBITMAP(nint value)
	{
		return new HBITMAP((void*)value);
	}

	public unsafe static implicit operator nint(HBITMAP value)
	{
		return (nint)value.Value;
	}

	public unsafe static explicit operator HBITMAP(sbyte value)
	{
		return new HBITMAP((void*)value);
	}

	public unsafe static explicit operator sbyte(HBITMAP value)
	{
		return (sbyte)value.Value;
	}

	public unsafe static explicit operator HBITMAP(ushort value)
	{
		return new HBITMAP((void*)value);
	}

	public unsafe static explicit operator ushort(HBITMAP value)
	{
		return (ushort)value.Value;
	}

	public unsafe static explicit operator HBITMAP(uint value)
	{
		return new HBITMAP((void*)value);
	}

	public unsafe static explicit operator uint(HBITMAP value)
	{
		return (uint)value.Value;
	}

	public unsafe static explicit operator HBITMAP(ulong value)
	{
		return new HBITMAP((void*)value);
	}

	public unsafe static explicit operator ulong(HBITMAP value)
	{
		return (ulong)value.Value;
	}

	public unsafe static explicit operator HBITMAP(nuint value)
	{
		return new HBITMAP((void*)value);
	}

	public unsafe static implicit operator nuint(HBITMAP value)
	{
		return (nuint)value.Value;
	}

	public int CompareTo(object obj)
	{
		if (obj is HBITMAP hBITMAP)
		{
			return CompareTo(hBITMAP);
		}
		if (obj != null)
		{
			throw new ArgumentException("obj is not an instance of HBITMAP.");
		}
		return 1;
	}

	public override bool Equals(object obj)
	{
		if (obj is HBITMAP other)
		{
			return Equals(other);
		}
		return false;
	}

	public unsafe bool Equals(HBITMAP other)
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
