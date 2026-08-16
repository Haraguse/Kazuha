using System;
using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Defines objects used in the template of a <see cref="T:FluentAvalonia.UI.Controls.FAMenuFlyoutItem" /> and related classes
/// </summary>
public sealed class FAMenuFlyoutItemTemplateSettings : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAMenuFlyoutItemTemplateSettings.Icon" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconElement> IconProperty = AvaloniaProperty.Register<FAMenuFlyoutItemTemplateSettings, FAIconElement>("Icon", (FAIconElement)null, false, (BindingMode)1, (Func<FAIconElement, bool>)null, (Func<AvaloniaObject, FAIconElement, FAIconElement>)null, false);

	/// <summary>
	/// Represents the FAIconElement for the MenuFlyoutItem
	/// </summary>
	public FAIconElement Icon
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconElement>(IconProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<FAIconElement>(IconProperty, value, (BindingPriority)0);
		}
	}
}
