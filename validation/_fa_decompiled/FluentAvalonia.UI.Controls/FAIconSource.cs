using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the base class for an icon source
/// </summary>
[TypeConverter(typeof(IconSourceConverter))]
public abstract class FAIconSource : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAIconSource.Foreground" /> property
	/// </summary>
	public static readonly StyledProperty<IBrush> ForegroundProperty = AvaloniaProperty.Register<FAIconSource, IBrush>("Foreground", (IBrush)null, false, (BindingMode)1, (Func<IBrush, bool>)null, (Func<AvaloniaObject, IBrush, IBrush>)null, false);

	/// <summary>
	/// Gets or sets a brush that describes the foreground color.
	/// </summary>
	public IBrush Foreground
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IBrush>(ForegroundProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IBrush>(ForegroundProperty, value, (BindingPriority)0);
		}
	}
}
