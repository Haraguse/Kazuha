using System;
using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources 
/// when defining templates for an InfoBadge.
/// </summary>
public sealed class FAInfoBadgeTemplateSettings : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBadgeTemplateSettings.InfoBadgeCornerRadius" /> property
	/// </summary>
	public static readonly StyledProperty<CornerRadius> InfoBadgeCornerRadiusProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBadgeTemplateSettings.IconElement" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconElement> IconElementProperty;

	/// <summary>
	/// Gets or sets the corner radius for an InfoBadge.
	/// </summary>
	public CornerRadius InfoBadgeCornerRadius
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<CornerRadius>(InfoBadgeCornerRadiusProperty);
		}
		internal set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<CornerRadius>(InfoBadgeCornerRadiusProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the icon element for an InfoBadge.
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

	static FAInfoBadgeTemplateSettings()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		InfoBadgeCornerRadiusProperty = AvaloniaProperty.Register<FAInfoBadgeTemplateSettings, CornerRadius>("InfoBadgeCornerRadius", default(CornerRadius), false, (BindingMode)1, (Func<CornerRadius, bool>)null, (Func<AvaloniaObject, CornerRadius, CornerRadius>)null, false);
		IconElementProperty = AvaloniaProperty.Register<FAInfoBadgeTemplateSettings, FAIconElement>("IconElement", (FAIconElement)null, false, (BindingMode)1, (Func<FAIconElement, bool>)null, (Func<AvaloniaObject, FAIconElement, FAIconElement>)null, false);
	}
}
