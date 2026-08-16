using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FluentAvalonia.Interop.Win32;

internal struct NCCALCSIZE_PARAMS
{
	public struct _rgrc_e__FixedBuffer
	{
		public RECT e0;

		public RECT e1;

		public RECT e2;

		public ref RECT this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ref AsSpan()[index];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<RECT> AsSpan()
		{
			return MemoryMarshal.CreateSpan(ref e0, 3);
		}
	}

	public _rgrc_e__FixedBuffer rgrc;

	public unsafe WINDOWPOS* lppos;
}
