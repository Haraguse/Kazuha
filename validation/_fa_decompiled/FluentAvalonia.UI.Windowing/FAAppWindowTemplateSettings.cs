using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;

namespace FluentAvalonia.UI.Windowing;

/// <summary>
/// Defines settings used in the template of an <see cref="T:FluentAvalonia.UI.Windowing.FAAppWindow" /> (Windows Only)
/// </summary>
public sealed class FAAppWindowTemplateSettings : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Windowing.FAAppWindowTemplateSettings.TitleBarHeight" /> property
	/// </summary>
	public static readonly StyledProperty<double> TitleBarHeightProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Windowing.FAAppWindowTemplateSettings.ContentMargin" /> property
	/// </summary>
	public static readonly StyledProperty<Thickness> ContentMarginProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Windowing.FAAppWindowTemplateSettings.IsTitleBarContentVisible" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsTitleBarContentVisibleProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Windowing.FAAppWindowTemplateSettings.WindowIcon" /> property
	/// </summary>
	public static readonly StyledProperty<IImage> WindowIconProperty;

	/// <summary>
	/// Gets or sets the height of the managed titlebar for AppWindow
	/// </summary>
	public double TitleBarHeight
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(TitleBarHeightProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(TitleBarHeightProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the window content margin for AppWindow
	/// </summary>
	/// <remarks>
	/// This value is calculated based on WindowState, title bar height and whether
	/// the content is extended into the titlebar area
	/// </remarks>
	public Thickness ContentMargin
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<Thickness>(ContentMarginProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<Thickness>(ContentMarginProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets whether the titlebar content is visible (Icon and App name text)
	/// </summary>
	public bool IsTitleBarContentVisible
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsTitleBarContentVisibleProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsTitleBarContentVisibleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the icon used in the managed titlebar of AppWindow
	/// </summary>
	public IImage WindowIcon
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IImage>(WindowIconProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IImage>(WindowIconProperty, value, (BindingPriority)0);
		}
	}

	static FAAppWindowTemplateSettings()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		TitleBarHeightProperty = AvaloniaProperty.Register<FAAppWindowTemplateSettings, double>("TitleBarHeight", 32.0, false, (BindingMode)1, (Func<double, bool>)null, (Func<AvaloniaObject, double, double>)null, false);
		ContentMarginProperty = AvaloniaProperty.Register<FAAppWindowTemplateSettings, Thickness>("ContentMargin", default(Thickness), false, (BindingMode)1, (Func<Thickness, bool>)null, (Func<AvaloniaObject, Thickness, Thickness>)null, false);
		IsTitleBarContentVisibleProperty = AvaloniaProperty.Register<FAAppWindowTemplateSettings, bool>("IsTitleBarContentVisible", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);
		WindowIconProperty = AvaloniaProperty.Register<FAAppWindowTemplateSettings, IImage>("WindowIcon", (IImage)null, false, (BindingMode)1, (Func<IImage, bool>)null, (Func<AvaloniaObject, IImage, IImage>)null, false);
	}
}
