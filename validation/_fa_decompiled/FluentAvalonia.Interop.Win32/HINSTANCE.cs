using System;

namespace FluentAvalonia.Interop.Win32;

internal unsafe readonly struct HINSTANCE(void* value) : IComparable, IEquatable<HINSTANCE>
{
	public unsafe readonly void* Value = value;

	public unsafe static HINSTANCE INVALID_VALUE => new HINSTANCE((void*)(-1));

	public unsafe static HINSTANCE NULL => new HINSTANCE(null);

	public unsafe static bool operator ==(HINSTANCE left, HINSTANCE right)
	{
		return left.Value == right.Value;
	}

	public unsafe static bool operator !=(HINSTANCE left, HINSTANCE right)
	{
		return left.Value != right.Value;
	}

	public unsafe static bool operator <(HINSTANCE left, HINSTANCE right)
	{
		return left.Value < right.Value;
	}

	public unsafe static bool operator <=(HINSTANCE left, HINSTANCE right)
	{
		return left.Value <= right.Value;
	}

	public unsafe static bool operator >(HINSTANCE left, HINSTANCE right)
	{
		return left.Value > right.Value;
	}

	public unsafe static bool operator >=(HINSTANCE left, HINSTANCE right)
	{
		return left.Value >= right.Value;
	}

	public unsafe static explicit operator HINSTANCE(void* value)
	{
		return new HINSTANCE(value);
	}

	public unsafe static implicit operator void*(HINSTANCE value)
	{
		return value.Value;
	}

	public unsafe static explicit operator HINSTANCE(byte value)
	{
		return new HINSTANCE((void*)value);
	}

	public unsafe static explicit operator byte(HINSTANCE value)
	{
		return (byte)value.Value;
	}

	public unsafe static explicit operator HINSTANCE(short value)
	{
		return new HINSTANCE((void*)value);
	}

	public unsafe static explicit operator short(HINSTANCE value)
	{
		return (short)value.Value;
	}

	public unsafe static explicit operator HINSTANCE(int value)
	{
		return new HINSTANCE((void*)value);
	}

	public unsafe static explicit operator int(HINSTANCE value)
	{
		return (int)value.Value;
	}

	public unsafe static explicit operator HINSTANCE(long value)
	{
		return new HINSTANCE((void*)value);
	}

	public unsafe static explicit operator long(HINSTANCE value)
	{
		return (long)value.Value;
	}

	public unsafe static explicit operator HINSTANCE(nint value)
	{
		return new HINSTANCE((void*)value);
	}

	public unsafe static implicit operator nint(HINSTANCE value)
	{
		return (nint)value.Value;
	}

	public unsafe static explicit operator HINSTANCE(sbyte value)
	{
		return new HINSTANCE((void*)value);
	}

	public unsafe static explicit operator sbyte(HINSTANCE value)
	{
		return (sbyte)value.Value;
	}

	public unsafe static explicit operator HINSTANCE(ushort value)
	{
		return new HINSTANCE((void*)value);
	}

	public unsafe static explicit operator ushort(HINSTANCE value)
	{
		return (ushort)value.Value;
	}

	public unsafe static explicit operator HINSTANCE(uint value)
	{
		return new HINSTANCE((void*)value);
	}

	public unsafe static explicit operator uint(HINSTANCE value)
	{
		return (uint)value.Value;
	}

	public unsafe static explicit operator HINSTANCE(ulong value)
	{
		return new HINSTANCE((void*)value);
	}

	public unsafe static explicit operator ulong(HINSTANCE value)
	{
		return (ulong)value.Value;
	}

	public unsafe static explicit operator HINSTANCE(nuint value)
	{
		return new HINSTANCE((void*)value);
	}

	public unsafe static implicit operator nuint(HINSTANCE value)
	{
		return (nuint)value.Value;
	}

	public int CompareTo(object obj)
	{
		if (obj is HINSTANCE hINSTANCE)
		{
			return CompareTo(hINSTANCE);
		}
		if (obj != null)
		{
			throw new ArgumentException("obj is not an instance of HINSTANCE.");
		}
		return 1;
	}

	public override bool Equals(object obj)
	{
		if (obj is HINSTANCE other)
		{
			return Equals(other);
		}
		return false;
	}

	public unsafe bool Equals(HINSTANCE other)
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
