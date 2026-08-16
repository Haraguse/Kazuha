using System;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Platform;

/// <summary>
/// A default implementation of <see cref="T:Avalonia.Platform.IPlatformSettings" /> for platforms.
/// </summary>
[PrivateApi]
public class DefaultPlatformSettings : IPlatformSettings
{
	private const int TouchTapSize = 10;

	private const int TouchDoubleTapSize = 50;

	public virtual TimeSpan HoldWaitDuration => TimeSpan.FromMilliseconds(300L);

	public PlatformHotkeyConfiguration HotkeyConfiguration => AvaloniaLocator.Current.GetRequiredService<PlatformHotkeyConfiguration>();

	public virtual event EventHandler<PlatformColorValues>? ColorValuesChanged;

	public virtual Size GetTapSize(PointerType type)
	{
		if ((uint)(type - 1) <= 1u)
		{
			return new Size(10.0, 10.0);
		}
		return new Size(4.0, 4.0);
	}

	public virtual Size GetDoubleTapSize(PointerType type)
	{
		if ((uint)(type - 1) <= 1u)
		{
			return new Size(50.0, 50.0);
		}
		return new Size(4.0, 4.0);
	}

	public virtual TimeSpan GetDoubleTapTime(PointerType type)
	{
		return TimeSpan.FromMilliseconds(500L);
	}

	public virtual PlatformColorValues GetColorValues()
	{
		return new PlatformColorValues
		{
			ThemeVariant = PlatformThemeVariant.Light
		};
	}

	protected void OnColorValuesChanged(PlatformColorValues colorValues)
	{
		Dispatcher.UIThread.Send(delegate
		{
			ColorValuesChanged?.Invoke(this, colorValues);
		});
	}
}
