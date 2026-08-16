using System;
using Avalonia.Threading;

namespace Avalonia.Utilities;

internal class NonPumpingLockHelper
{
	public interface IHelperImpl
	{
		int Wait(nint[] waitHandles, bool waitAll, int millisecondsTimeout);
	}

	public static IDisposable? Use()
	{
		IHelperImpl service = AvaloniaLocator.Current.GetService<IHelperImpl>();
		if (service == null)
		{
			return null;
		}
		return NonPumpingSyncContext.Use(service);
	}
}
