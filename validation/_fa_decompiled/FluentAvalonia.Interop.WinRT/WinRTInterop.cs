using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using FluentAvalonia.Interop.Win32;
using MicroCom.Runtime;

namespace FluentAvalonia.Interop.WinRT;

internal static class WinRTInterop
{
	internal enum RO_INIT_TYPE
	{
		RO_INIT_SINGLETHREADED,
		RO_INIT_MULTITHREADED
	}

	private static bool _initialized;

	[LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
	[UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	internal unsafe static int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString, uint length, out nint hstring)
	{
		hstring = 0;
		int result;
		fixed (nint* _hstring_native = &hstring)
		{
			fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(sourceString))
			{
				void* _sourceString_native = ptr;
				result = __PInvoke((ushort*)_sourceString_native, length, _hstring_native);
			}
		}
		return result;
		[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", EntryPoint = "WindowsCreateString", ExactSpelling = true)]
		[UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
		unsafe static extern int __PInvoke(ushort* __sourceString_native, uint __length_native, nint* __hstring_native);
	}

	[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", ExactSpelling = true)]
	[LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
	[UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	internal unsafe static extern char* WindowsGetStringRawBuffer(nint hstring, uint* length);

	internal unsafe static string GetString(nint hString)
	{
		uint length = default(uint);
		return new string(WindowsGetStringRawBuffer(hString, &length), 0, (int)length);
	}

	[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", ExactSpelling = true)]
	[LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
	[UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	internal static extern BOOL WindowsIsStringEmpty(nint @string);

	internal static nint WindowsCreateString(string sourceString)
	{
		if (sourceString == null)
		{
			throw new ArgumentNullException("sourceString");
		}
		int num = WindowsCreateString(sourceString, (uint)sourceString.Length, out var hstring);
		if (num < 0)
		{
			throw new InvalidOperationException($"WindowsCreateString failed with HRESULT: 0x{num:X8}");
		}
		return hstring;
	}

	[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", ExactSpelling = true)]
	[LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
	[UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	internal static extern void WindowsDeleteString(nint hString);

	internal static T CreateInstance<T>(string fullName) where T : IUnknown
	{
		nint num = WindowsCreateString(fullName);
		EnsureRoInitialized();
		int num2 = RoActivateInstance(num, out var instance);
		if (num2 < 0)
		{
			WindowsDeleteString(num);
			throw new COMException("RoActivateInstance failed", num2);
		}
		IUnknown val = MicroComRuntime.CreateProxyFor<IUnknown>((IntPtr)instance, true);
		try
		{
			WindowsDeleteString(num);
			return MicroComRuntime.QueryInterface<T>(val);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void EnsureRoInitialized()
	{
		if (!_initialized)
		{
			int num = RoInitialize((Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) ? RO_INIT_TYPE.RO_INIT_MULTITHREADED : RO_INIT_TYPE.RO_INIT_SINGLETHREADED);
			if (num < 0)
			{
				throw new InvalidOperationException($"RoInitialize failed with HRESULT: 0x{num:X8}");
			}
			_initialized = true;
		}
	}

	[DllImport("combase.dll", ExactSpelling = true)]
	[LibraryImport("combase.dll")]
	private static extern int RoInitialize(RO_INIT_TYPE initType);

	[LibraryImport("combase.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	private unsafe static int RoActivateInstance(nint activatableClassId, out nint instance)
	{
		instance = 0;
		int result;
		fixed (nint* _instance_native = &instance)
		{
			result = __PInvoke(activatableClassId, _instance_native);
		}
		return result;
		[DllImport("combase.dll", EntryPoint = "RoActivateInstance", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __activatableClassId_native, nint* __instance_native);
	}

	[LibraryImport("combase.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.27113")]
	[SkipLocalsInit]
	private unsafe static int RoGetActivationFactory(nint activatableClassId, ref Guid iid, out nint factory)
	{
		factory = 0;
		int result;
		fixed (nint* _factory_native = &factory)
		{
			fixed (Guid* _iid_native = &iid)
			{
				result = __PInvoke(activatableClassId, _iid_native, _factory_native);
			}
		}
		return result;
		[DllImport("combase.dll", EntryPoint = "RoGetActivationFactory", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __activatableClassId_native, Guid* __iid_native, nint* __factory_native);
	}
}
