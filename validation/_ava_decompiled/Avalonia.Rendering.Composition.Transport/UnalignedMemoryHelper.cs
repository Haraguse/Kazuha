using System.Runtime.CompilerServices;

namespace Avalonia.Rendering.Composition.Transport;

internal static class UnalignedMemoryHelper
{
	public unsafe static T ReadUnaligned<T>(byte* src) where T : unmanaged
	{
		Unsafe.SkipInit<T>(out var value);
		UnalignedMemcpy((byte*)(&value), src, Unsafe.SizeOf<T>());
		return value;
	}

	public unsafe static void WriteUnaligned<T>(byte* dst, T value) where T : unmanaged
	{
		UnalignedMemcpy(dst, (byte*)(&value), Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private unsafe static void UnalignedMemcpy(byte* dst, byte* src, int count)
	{
		for (int i = 0; i < count; i++)
		{
			dst[i] = src[i];
		}
	}
}
