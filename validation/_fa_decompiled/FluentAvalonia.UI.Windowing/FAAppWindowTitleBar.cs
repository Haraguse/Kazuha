using Avalonia.Media;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Windowing;

/// <summary>
/// Represents the title bar of an <see cref="T:FluentAvalonia.UI.Windowing.FAAppWindow" /> allowing customization such as
/// colors, hit testing, and allowing app content in the title bar area
/// </summary>
public class FAAppWindowTitleBar
{
	private FAAppWindow _parent;

	private Color? _backgroundColor;

	private Color? _buttonBackgroundColor;

	private Color? _buttonForegroundColor;

	private Color? _buttonHoverBackgroundColor;

	private Color? _buttonHoverForegroundColor;

	private Color? _buttonInactiveBackgroundColor;

	private Color? _buttonInactiveForegroundColor;

	private Color? _buttonPressedBackgroundColor;

	private Color? _buttonPressedForegroundColor;

	private bool _extendsContentIntoTitleBar;

	private Color? _foregroundColor;

	private double _height = 32.0;

	private Color? _inactiveBackgroundColor;

	private Color? _inactiveForegroundColor;

	private bool _showFullScreenButton;

	/// <summary>
	/// Gets or sets the background color of the title bar when the window is active
	/// </summary>
	public Color? BackgroundColor
	{
		get
		{
			return _backgroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? backgroundColor = _backgroundColor;
			Color? val = value;
			if (backgroundColor.HasValue != val.HasValue || (backgroundColor.HasValue && backgroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_backgroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the foreground color of the title bar when the window is active
	/// </summary>
	public Color? ForegroundColor
	{
		get
		{
			return _foregroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? foregroundColor = _foregroundColor;
			Color? val = value;
			if (foregroundColor.HasValue != val.HasValue || (foregroundColor.HasValue && foregroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_foregroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the background color of the title bar when the window is inactive
	/// </summary>
	public Color? InactiveBackgroundColor
	{
		get
		{
			return _inactiveBackgroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? inactiveBackgroundColor = _inactiveBackgroundColor;
			Color? val = value;
			if (inactiveBackgroundColor.HasValue != val.HasValue || (inactiveBackgroundColor.HasValue && inactiveBackgroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_inactiveBackgroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the foreground color of the title bar when the window is inactive
	/// </summary>
	public Color? InactiveForegroundColor
	{
		get
		{
			return _inactiveForegroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? inactiveForegroundColor = _inactiveForegroundColor;
			Color? val = value;
			if (inactiveForegroundColor.HasValue != val.HasValue || (inactiveForegroundColor.HasValue && inactiveForegroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_inactiveForegroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the background color of the caption buttons when the window is active
	/// </summary>
	public Color? ButtonBackgroundColor
	{
		get
		{
			return _buttonBackgroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? buttonBackgroundColor = _buttonBackgroundColor;
			Color? val = value;
			if (buttonBackgroundColor.HasValue != val.HasValue || (buttonBackgroundColor.HasValue && buttonBackgroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_buttonBackgroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the foreground color of the caption buttons when the window is active
	/// </summary>
	public Color? ButtonForegroundColor
	{
		get
		{
			return _buttonForegroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? buttonForegroundColor = _buttonForegroundColor;
			Color? val = value;
			if (buttonForegroundColor.HasValue != val.HasValue || (buttonForegroundColor.HasValue && buttonForegroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_buttonForegroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the background color of the caption buttons when the window is active
	/// and the pointer is over the minimize or maximize button
	/// </summary>
	public Color? ButtonHoverBackgroundColor
	{
		get
		{
			return _buttonHoverBackgroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? buttonHoverBackgroundColor = _buttonHoverBackgroundColor;
			Color? val = value;
			if (buttonHoverBackgroundColor.HasValue != val.HasValue || (buttonHoverBackgroundColor.HasValue && buttonHoverBackgroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_buttonHoverBackgroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the foreground color of the caption buttons when the window is active
	/// and the pointer is over the minimize or maximize button
	/// </summary>
	public Color? ButtonHoverForegroundColor
	{
		get
		{
			return _buttonHoverForegroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? buttonHoverForegroundColor = _buttonHoverForegroundColor;
			Color? val = value;
			if (buttonHoverForegroundColor.HasValue != val.HasValue || (buttonHoverForegroundColor.HasValue && buttonHoverForegroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_buttonHoverForegroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the background color of the caption buttons when the window is active
	/// and the pointer is pressed on the minimize or maximize button
	/// </summary>
	public Color? ButtonPressedBackgroundColor
	{
		get
		{
			return _buttonPressedBackgroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? buttonPressedBackgroundColor = _buttonPressedBackgroundColor;
			Color? val = value;
			if (buttonPressedBackgroundColor.HasValue != val.HasValue || (buttonPressedBackgroundColor.HasValue && buttonPressedBackgroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_buttonPressedBackgroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the foreground color of the caption buttons when the window is active
	/// and the pointer is pressed on the minimize or maximize button
	/// </summary>
	public Color? ButtonPressedForegroundColor
	{
		get
		{
			return _buttonPressedForegroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? buttonPressedForegroundColor = _buttonPressedForegroundColor;
			Color? val = value;
			if (buttonPressedForegroundColor.HasValue != val.HasValue || (buttonPressedForegroundColor.HasValue && buttonPressedForegroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_buttonPressedForegroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the background color of the caption buttons when the window is inactive
	/// </summary>
	public Color? ButtonInactiveBackgroundColor
	{
		get
		{
			return _buttonInactiveBackgroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? buttonInactiveBackgroundColor = _buttonInactiveBackgroundColor;
			Color? val = value;
			if (buttonInactiveBackgroundColor.HasValue != val.HasValue || (buttonInactiveBackgroundColor.HasValue && buttonInactiveBackgroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_buttonInactiveBackgroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the foreground color of the caption buttons when the window is inactive
	/// </summary>
	public Color? ButtonInactiveForegroundColor
	{
		get
		{
			return _buttonInactiveForegroundColor;
		}
		set
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Color? buttonInactiveForegroundColor = _buttonInactiveForegroundColor;
			Color? val = value;
			if (buttonInactiveForegroundColor.HasValue != val.HasValue || (buttonInactiveForegroundColor.HasValue && buttonInactiveForegroundColor.GetValueOrDefault() != val.GetValueOrDefault()))
			{
				_buttonInactiveForegroundColor = value;
				_parent.TitleBarColorsChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the window content should display in the title bar area of the window
	/// </summary>
	public bool ExtendsContentIntoTitleBar
	{
		get
		{
			return _extendsContentIntoTitleBar;
		}
		set
		{
			if (_extendsContentIntoTitleBar != value)
			{
				_extendsContentIntoTitleBar = value;
				_parent.OnExtendsContentIntoTitleBarChanged(value);
			}
		}
	}

	/// <summary>
	/// Gets or sets the height of the default title bar
	/// </summary>
	/// <remarks>
	/// If <see cref="P:FluentAvalonia.UI.Windowing.FAAppWindowTitleBar.ExtendsContentIntoTitleBar" /> is true, this value describes the height of the
	/// default drag rect and caption buttons only. If custom drag rects are set, only the caption
	/// buttons are affected by this
	/// </remarks>
	public double Height
	{
		get
		{
			return _height;
		}
		set
		{
			if (!FAMathHelpers.IsClose(_height, value))
			{
				_height = value;
				_parent.OnTitleBarHeightChanged(value);
			}
		}
	}

	/// <summary>
	/// Sets whether the full screen button is visible in the title bar.
	/// </summary>
	public bool ShowFullScreenButton
	{
		get
		{
			return _showFullScreenButton;
		}
		set
		{
			if (_showFullScreenButton != value)
			{
				_showFullScreenButton = value;
				_parent.OnShowFullScreenButtonChanged(value);
			}
		}
	}

	internal FAAppWindowTitleBar(FAAppWindow parent)
	{
		_parent = parent;
	}
}
