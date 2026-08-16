using System;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using FluentAvalonia.Interop;
using FluentAvalonia.Interop.Win32;
using FluentAvalonia.Interop.WinRT;

namespace FluentAvalonia.UI.Windowing;

internal class Win32AppWindowFeatures : IFAAppWindowPlatformFeatures
{
	private FAAppWindow _owner;

	private ITaskbarList3 _taskBarList;

	public Win32AppWindowFeatures(FAAppWindow owner)
	{
		_owner = owner;
	}

	public void SetTaskBarProgressBarState(FATaskBarProgressBarState state)
	{
		if (_taskBarList == null)
		{
			CreateTaskBarList();
			if (_taskBarList == null)
			{
				return;
			}
		}
		_taskBarList.SetProgressState(((TopLevel)_owner).TryGetPlatformHandle().Handle, (int)state);
	}

	public void SetTaskBarProgressBarValue(ulong currentValue, ulong totalValue)
	{
		if (_taskBarList == null)
		{
			CreateTaskBarList();
			if (_taskBarList == null)
			{
				return;
			}
		}
		_taskBarList.SetProgressValue(((TopLevel)_owner).TryGetPlatformHandle().Handle, currentValue, totalValue);
	}

	public unsafe void SetWindowBorderColor(Color color)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		if (!OSVersionHelper.IsWindows11())
		{
			return;
		}
		COLORREF cOLORREF = (uint)(0 | (((Color)(ref color)).B << 16) | (((Color)(ref color)).G << 8) | ((Color)(ref color)).R);
		HRESULT hRESULT = Win32Interop.DwmSetWindowAttribute(((TopLevel)_owner).TryGetPlatformHandle().Handle, DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR, &cOLORREF, sizeof(COLORREF));
		if (!hRESULT.SUCCEEDED)
		{
			ParametrizedLogger? val = Logger.TryGet((LogEventLevel)1, "AppWindow");
			if (val.HasValue)
			{
				ParametrizedLogger valueOrDefault = val.GetValueOrDefault();
				((ParametrizedLogger)(ref valueOrDefault)).Log<HRESULT>((object)"SetWindowBorderColor", "Failed to set the border color of the window with hr: {hr}", hRESULT);
			}
		}
	}

	private void CreateTaskBarList()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			_taskBarList = Win32Interop.CreateInstance<ITaskbarList3>(Win32Interop.ITaskBarList3CLSID, Win32Interop.ITaskBarList3IID);
		}
		catch (Exception ex)
		{
			ParametrizedLogger? val = Logger.TryGet((LogEventLevel)1, "AppWindow");
			if (val.HasValue)
			{
				ParametrizedLogger valueOrDefault = val.GetValueOrDefault();
				((ParametrizedLogger)(ref valueOrDefault)).Log<Exception>((object)"SetWindowBorderColor", "Unable to create instance of ITaskbarList3", ex);
			}
		}
	}
}
