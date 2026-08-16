using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;

namespace FluentAvalonia.UI.Controls;

public class FATabViewItemTemplateSettings : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabViewItemTemplateSettings.IconElement" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconElement> IconElementProperty = AvaloniaProperty.Register<FATabViewItemTemplateSettings, FAIconElement>("IconElement", (FAIconElement)null, false, (BindingMode)1, (Func<FAIconElement, bool>)null, (Func<AvaloniaObject, FAIconElement, FAIconElement>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATabViewItemTemplateSettings.TabGeometry" /> property
	/// </summary>
	public static readonly StyledProperty<Geometry> TabGeometryProperty = AvaloniaProperty.Register<FATabViewItemTemplateSettings, Geometry>("TabGeometry", (Geometry)null, false, (BindingMode)1, (Func<Geometry, bool>)null, (Func<AvaloniaObject, Geometry, Geometry>)null, false);

	/// <summary>
	/// Gets the IconElement that relates to the IconSource of the current TabViewItem
	/// </summary>
	public FAIconElement IconElement
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconElement>(IconElementProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<FAIconElement>(IconElementProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the geometry of the current TabViewItem
	/// </summary>
	public Geometry TabGeometry
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Geometry>(TabGeometryProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<Geometry>(TabGeometryProperty, value, (BindingPriority)0);
		}
	}
}
