using System;
using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents data for use in a SettingsExpander temlate
/// </summary>
public sealed class FASettingsExpanderTemplateSettings : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderTemplateSettings.Icon" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconElement> IconProperty = AvaloniaProperty.Register<FASettingsExpanderTemplateSettings, FAIconElement>("Icon", (FAIconElement)null, false, (BindingMode)1, (Func<FAIconElement, bool>)null, (Func<AvaloniaObject, FAIconElement, FAIconElement>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASettingsExpanderTemplateSettings.ActionIcon" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconElement> ActionIconProperty = AvaloniaProperty.Register<FASettingsExpanderTemplateSettings, FAIconElement>("ActionIcon", (FAIconElement)null, false, (BindingMode)1, (Func<FAIconElement, bool>)null, (Func<AvaloniaObject, FAIconElement, FAIconElement>)null, false);

	/// <summary>
	/// Defines the FAIconElement to be used for the SettingsExpander
	/// </summary>
	public FAIconElement Icon
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconElement>(IconProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAIconElement>(IconProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Defines the FAIconElement to be used for the SettingsExpander ActionIcon
	/// </summary>
	public FAIconElement ActionIcon
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconElement>(ActionIconProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAIconElement>(ActionIconProperty, value, (BindingPriority)0);
		}
	}

	internal FASettingsExpanderTemplateSettings()
	{
	}
}
