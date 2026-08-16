using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a control for indicating notifications, alerts, new content, 
/// or to attract focus to an area within an app.
/// </summary>
/// <summary>
/// Represents a control for indicating notifications, alerts, new content, 
/// or to attract focus to an area within an app.
/// </summary>
[PseudoClasses(new string[] { ":value", ":fonticon", ":icon", ":dot" })]
public class FAInfoBadge : TemplatedControl
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBadge.Value" /> property
	/// </summary>
	public static readonly StyledProperty<int> ValueProperty = AvaloniaProperty.Register<FAInfoBadge, int>("Value", -1, false, (BindingMode)1, (Func<int, bool>)null, (Func<AvaloniaObject, int, int>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBadge.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = FASettingsExpander.IconSourceProperty.AddOwner<FAInfoBadge>((StyledPropertyMetadata<FAIconSource>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAInfoBadge.TemplateSettings" /> property
	/// </summary>
	public static readonly StyledProperty<FAInfoBadgeTemplateSettings> TemplateSettingsProperty = AvaloniaProperty.Register<FAInfoBadge, FAInfoBadgeTemplateSettings>("TemplateSettings", (FAInfoBadgeTemplateSettings)null, false, (BindingMode)1, (Func<FAInfoBadgeTemplateSettings, bool>)null, (Func<AvaloniaObject, FAInfoBadgeTemplateSettings, FAInfoBadgeTemplateSettings>)null, false);

	private const string s_pcValue = ":value";

	private const string s_pcFontIcon = ":fonticon";

	private const string s_pcDot = ":dot";

	/// <summary>
	/// Gets or sets the integer to be displayed in a numeric InfoBadge.
	/// </summary>
	public int Value
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<int>(ValueProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<int>(ValueProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the icon to be used in an InfoBadge.
	/// </summary>
	public FAIconSource IconSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconSource>(IconSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAIconSource>(IconSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Provides calculated values that can be referenced as TemplatedParent sources when defining 
	/// templates for an InfoBadge. Not intended for general use.
	/// </summary>
	public FAInfoBadgeTemplateSettings TemplateSettings
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAInfoBadgeTemplateSettings>(TemplateSettingsProperty);
		}
		internal set
		{
			((AvaloniaObject)this).SetValue<FAInfoBadgeTemplateSettings>(TemplateSettingsProperty, value, (BindingPriority)0);
		}
	}

	public FAInfoBadge()
	{
		TemplateSettings = new FAInfoBadgeTemplateSettings();
		((Control)this).SizeChanged += HandleSizeChanged;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		((TemplatedControl)this).OnApplyTemplate(e);
		OnDisplayKindPropertiesChanged();
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		Size result = ((Layoutable)this).MeasureOverride(availableSize);
		if (((Size)(ref result)).Width < ((Size)(ref result)).Height)
		{
			return new Size(((Size)(ref result)).Height, ((Size)(ref result)).Height);
		}
		return result;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((Control)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)ValueProperty)
		{
			if (Value < -1)
			{
				throw new ArgumentOutOfRangeException("Value");
			}
		}
		else if (change.Property == (AvaloniaProperty)(object)ValueProperty || change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			OnDisplayKindPropertiesChanged();
		}
	}

	private void OnDisplayKindPropertiesChanged()
	{
		FAIconSource iconSource = IconSource;
		if (Value >= 0)
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":value", true);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":fonticon", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":dot", false);
		}
		else if (iconSource != null)
		{
			TemplateSettings.IconElement = FAIconHelpers.CreateFromUnknown(iconSource);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":fonticon", iconSource is FAFontIconSource);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", !(iconSource is FAFontIconSource));
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":value", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":dot", false);
		}
		else
		{
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":dot", true);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":value", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":fonticon", false);
			PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":icon", false);
		}
	}

	private void HandleSizeChanged(object sender, SizeChangedEventArgs args)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		Size newSize = args.NewSize;
		double num = ((Size)(ref newSize)).Height * 0.5;
		if (!((AvaloniaObject)this).IsSet((AvaloniaProperty)(object)TemplatedControl.CornerRadiusProperty))
		{
			TemplateSettings.InfoBadgeCornerRadius = new CornerRadius(num);
		}
		else
		{
			TemplateSettings.InfoBadgeCornerRadius = default(CornerRadius);
		}
	}
}
