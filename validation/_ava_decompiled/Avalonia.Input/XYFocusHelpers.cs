using System;
using Avalonia.VisualTree;

namespace Avalonia.Input;

internal static class XYFocusHelpers
{
	internal static bool IsAllowedXYNavigationMode(this InputElement visual, KeyDeviceType? keyDeviceType)
	{
		return IsAllowedXYNavigationMode(XYFocus.GetNavigationModes(visual), keyDeviceType);
	}

	private static bool IsAllowedXYNavigationMode(XYFocusNavigationModes modes, KeyDeviceType? keyDeviceType)
	{
		return keyDeviceType switch
		{
			null => !modes.Equals(XYFocusNavigationModes.Disabled), 
			KeyDeviceType.Keyboard => modes.HasFlag(XYFocusNavigationModes.Keyboard), 
			KeyDeviceType.Gamepad => modes.HasFlag(XYFocusNavigationModes.Gamepad), 
			KeyDeviceType.Remote => modes.HasFlag(XYFocusNavigationModes.Remote), 
			_ => throw new ArgumentOutOfRangeException("keyDeviceType", keyDeviceType, null), 
		};
	}

	internal static InputElement? FindXYSearchRoot(this InputElement visual, KeyDeviceType? keyDeviceType)
	{
		InputElement inputElement = visual;
		InputElement inputElement2 = visual.FindAncestorOfType<InputElement>();
		while (inputElement2 != null && inputElement2.IsAllowedXYNavigationMode(keyDeviceType))
		{
			inputElement = inputElement2;
			inputElement2 = inputElement.FindAncestorOfType<InputElement>();
		}
		return inputElement;
	}
}
