using System;
using Avalonia;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when 
/// defining templates for a <see cref="T:FluentAvalonia.UI.Controls.FATeachingTip" />.
/// </summary>
public sealed class FATeachingTipTemplateSettings : AvaloniaObject
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTipTemplateSettings.TopRightHighlightMargin" /> property
	/// </summary>
	public static readonly StyledProperty<Thickness> TopRightHighlighMarginProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTipTemplateSettings.TopLeftHighlightMargin" /> property
	/// </summary>
	public static readonly StyledProperty<Thickness> TopLeftHighlightMarginProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTipTemplateSettings.IconElement" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconElement> IconElementProperty;

	/// <summary>
	/// Gets the thickness value of the top right highlight margin.
	/// </summary>
	public Thickness TopRightHighlightMargin
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<Thickness>(TopRightHighlighMarginProperty);
		}
		internal set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<Thickness>(TopRightHighlighMarginProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the thickness value of the top left highlight margin.
	/// </summary>
	public Thickness TopLeftHighlightMargin
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<Thickness>(TopLeftHighlightMarginProperty);
		}
		internal set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<Thickness>(TopLeftHighlightMarginProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets the icon element.
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

	static FATeachingTipTemplateSettings()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		TopRightHighlighMarginProperty = AvaloniaProperty.Register<FATeachingTipTemplateSettings, Thickness>("TopRightHighlightMargin", default(Thickness), false, (BindingMode)1, (Func<Thickness, bool>)null, (Func<AvaloniaObject, Thickness, Thickness>)null, false);
		TopLeftHighlightMarginProperty = AvaloniaProperty.Register<FATeachingTipTemplateSettings, Thickness>("TopLeftHighlightMargin", default(Thickness), false, (BindingMode)1, (Func<Thickness, bool>)null, (Func<AvaloniaObject, Thickness, Thickness>)null, false);
		IconElementProperty = AvaloniaProperty.Register<FATeachingTipTemplateSettings, FAIconElement>("IconElement", (FAIconElement)null, false, (BindingMode)1, (Func<FAIconElement, bool>)null, (Func<AvaloniaObject, FAIconElement, FAIconElement>)null, false);
	}
}
