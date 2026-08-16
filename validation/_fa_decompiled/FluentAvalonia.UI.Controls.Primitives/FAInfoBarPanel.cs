using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls.Primitives;

/// <summary>
/// Represents a panel that arranges its items horizontally if there is available space, otherwise vertically.
/// </summary>
/// <remarks>
/// This control is specific to the <see cref="T:FluentAvalonia.UI.Controls.FAInfoBar" /> and generally should not be used elsewhere
/// </remarks>
/// <summary>
/// Represents a panel that arranges its items horizontally if there is available space, otherwise vertically.
/// </summary>
/// <remarks>
/// This control is specific to the <see cref="T:FluentAvalonia.UI.Controls.FAInfoBar" /> and generally should not be used elsewhere
/// </remarks>
public sealed class FAInfoBarPanel : Panel
{
	private bool _isVertical;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FAInfoBarPanel.HorizontalOrientationPadding" /> property
	/// </summary>
	public static readonly StyledProperty<Thickness> HorizontalOrientationPaddingProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FAInfoBarPanel.VerticalOrientationPadding" /> property
	/// </summary>
	public static readonly StyledProperty<Thickness> VerticalOrientationPaddingProperty;

	/// <summary>
	/// Defines the HorizontalOrientationMargin attached property
	/// </summary>
	public static readonly AttachedProperty<Thickness> HorizontalOrientationMarginProperty;

	/// <summary>
	/// Defines the VerticalOrientationMargin attached property
	/// </summary>
	public static readonly AttachedProperty<Thickness> VerticalOrientationMarginProperty;

	/// <summary>
	/// Gets and sets the distance between the edges of the InfoBarPanel and its children when the 
	/// panel is oriented horizontally.
	/// </summary>
	public Thickness HorizontalOrientationPadding
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<Thickness>(HorizontalOrientationPaddingProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<Thickness>(HorizontalOrientationPaddingProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets and sets the distance between the edges of the InfoBarPanel and its children when the
	/// panel is oriented vertically.
	/// </summary>
	public Thickness VerticalOrientationPadding
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<Thickness>(VerticalOrientationPaddingProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<Thickness>(VerticalOrientationPaddingProperty, value, (BindingPriority)0);
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.0;
		int num6 = 0;
		StyledElement parent = ((StyledElement)this).Parent;
		Control val = (Control)(object)((parent is Control) ? parent : null);
		double num7 = ((val == null) ? 0.0 : (((Layoutable)val).MinHeight - ((Layoutable)this).Margin.Vertical()));
		Controls children = ((Panel)this).Children;
		int count = ((AvaloniaList<Control>)(object)children).Count;
		for (int i = 0; i < ((AvaloniaList<Control>)(object)children).Count; i++)
		{
			((Layoutable)((AvaloniaList<Control>)(object)children)[i]).Measure(availableSize);
			Size desiredSize = ((Layoutable)((AvaloniaList<Control>)(object)children)[i]).DesiredSize;
			if (((Size)(ref desiredSize)).Width != 0.0 && ((Size)(ref desiredSize)).Height != 0.0)
			{
				Thickness horizontalOrientationMargin = GetHorizontalOrientationMargin(((AvaloniaList<Control>)(object)children)[i]);
				num += ((Size)(ref desiredSize)).Width + ((num6 > 0) ? ((Thickness)(ref horizontalOrientationMargin)).Left : 0.0) + ((num6 < count - 1) ? ((Thickness)(ref horizontalOrientationMargin)).Right : 0.0);
				Thickness verticalOrientationMargin = GetVerticalOrientationMargin(((AvaloniaList<Control>)(object)children)[i]);
				num2 += ((Size)(ref desiredSize)).Height + ((num6 > 0) ? ((Thickness)(ref verticalOrientationMargin)).Top : 0.0) + ((num6 < count - 1) ? ((Thickness)(ref verticalOrientationMargin)).Bottom : 0.0);
				if (((Size)(ref desiredSize)).Width > num3)
				{
					num3 = ((Size)(ref desiredSize)).Width;
				}
				if (((Size)(ref desiredSize)).Height > num4)
				{
					num4 = ((Size)(ref desiredSize)).Height;
				}
				double num8 = ((Size)(ref desiredSize)).Height + horizontalOrientationMargin.Vertical();
				if (num8 > num5)
				{
					num5 = num8;
				}
				num6++;
			}
		}
		if (num6 == 1 || num > ((Size)(ref availableSize)).Width || (num7 > 0.0 && num5 > num7))
		{
			_isVertical = true;
			Thickness verticalOrientationPadding = VerticalOrientationPadding;
			return new Size(num3 + verticalOrientationPadding.Horizontal(), num2 + verticalOrientationPadding.Vertical());
		}
		_isVertical = false;
		Thickness horizontalOrientationPadding = HorizontalOrientationPadding;
		return new Size(num + horizontalOrientationPadding.Horizontal(), num4 + horizontalOrientationPadding.Vertical());
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		Size result = finalSize;
		if (_isVertical)
		{
			Thickness verticalOrientationPadding = VerticalOrientationPadding;
			double num = ((Thickness)(ref verticalOrientationPadding)).Top;
			bool flag = false;
			for (int i = 0; i < ((AvaloniaList<Control>)(object)((Panel)this).Children).Count; i++)
			{
				Size desiredSize = ((Layoutable)((AvaloniaList<Control>)(object)((Panel)this).Children)[i]).DesiredSize;
				if (((Size)(ref desiredSize)).Width != 0.0 && ((Size)(ref desiredSize)).Height != 0.0)
				{
					Thickness verticalOrientationMargin = GetVerticalOrientationMargin(((AvaloniaList<Control>)(object)((Panel)this).Children)[i]);
					num += (flag ? ((Thickness)(ref verticalOrientationMargin)).Top : 0.0);
					((Layoutable)((AvaloniaList<Control>)(object)((Panel)this).Children)[i]).Arrange(new Rect(((Thickness)(ref verticalOrientationPadding)).Left + ((Thickness)(ref verticalOrientationMargin)).Left, num, ((Size)(ref desiredSize)).Width, ((Size)(ref desiredSize)).Height));
					num += ((Size)(ref desiredSize)).Height + ((Thickness)(ref verticalOrientationMargin)).Bottom;
					flag = true;
				}
			}
		}
		else
		{
			Thickness horizontalOrientationPadding = HorizontalOrientationPadding;
			double num2 = ((Thickness)(ref horizontalOrientationPadding)).Left;
			bool flag2 = false;
			int count = ((AvaloniaList<Control>)(object)((Panel)this).Children).Count;
			for (int j = 0; j < count; j++)
			{
				Size desiredSize2 = ((Layoutable)((AvaloniaList<Control>)(object)((Panel)this).Children)[j]).DesiredSize;
				if (((Size)(ref desiredSize2)).Width != 0.0 && ((Size)(ref desiredSize2)).Height != 0.0)
				{
					Thickness horizontalOrientationMargin = GetHorizontalOrientationMargin(((AvaloniaList<Control>)(object)((Panel)this).Children)[j]);
					num2 += (flag2 ? ((Thickness)(ref horizontalOrientationMargin)).Left : 0.0);
					if (j < count - 1)
					{
						((Layoutable)((AvaloniaList<Control>)(object)((Panel)this).Children)[j]).Arrange(new Rect(num2, ((Thickness)(ref horizontalOrientationPadding)).Top + ((Thickness)(ref horizontalOrientationMargin)).Top, ((Size)(ref desiredSize2)).Width, ((Size)(ref desiredSize2)).Height));
					}
					else
					{
						((Layoutable)((AvaloniaList<Control>)(object)((Panel)this).Children)[j]).Arrange(new Rect(num2, ((Thickness)(ref horizontalOrientationPadding)).Top + ((Thickness)(ref horizontalOrientationMargin)).Top, Math.Max(((Size)(ref desiredSize2)).Width, ((Size)(ref finalSize)).Width - num2), ((Size)(ref desiredSize2)).Height));
					}
					num2 += ((Size)(ref desiredSize2)).Width + ((Thickness)(ref horizontalOrientationMargin)).Right;
					flag2 = true;
				}
			}
		}
		return result;
	}

	/// <summary>
	/// Sets the HorizontalOrientationMargin to an object.
	/// </summary>
	/// <param name="c">The IControl to set the property on</param>
	/// <param name="t">The desired Thickness</param>
	public static void SetHorizontalOrientationMargin(Control c, Thickness t)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		((AvaloniaObject)c).SetValue<Thickness>((StyledProperty<Thickness>)(object)HorizontalOrientationMarginProperty, t, (BindingPriority)0);
	}

	/// <summary>
	/// Gets the HorizontalOrientationMargin from an object.
	/// </summary>
	/// <param name="c">The IControl to retreive the value from</param>
	/// <returns>The HorizontalOrientationMargin thickness</returns>
	public static Thickness GetHorizontalOrientationMargin(Control c)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return ((AvaloniaObject)c).GetValue<Thickness>((StyledProperty<Thickness>)(object)HorizontalOrientationMarginProperty);
	}

	/// <summary>
	/// Sets the VerticalOrientationMargin to an object.
	/// </summary>
	/// <param name="c">The IControl to set the property on</param>
	/// <param name="t">The desired Thickness</param>
	public static void SetVerticalOrientationMargin(Control c, Thickness t)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		((AvaloniaObject)c).SetValue<Thickness>((StyledProperty<Thickness>)(object)VerticalOrientationMarginProperty, t, (BindingPriority)0);
	}

	/// <summary>
	/// Gets the VerticalOrientationMargin from an object.
	/// </summary>
	/// <param name="c">The IControl to retreive the value from</param>
	/// <returns>The VerticalOrientationMargin thickness</returns>
	public static Thickness GetVerticalOrientationMargin(Control c)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return ((AvaloniaObject)c).GetValue<Thickness>((StyledProperty<Thickness>)(object)VerticalOrientationMarginProperty);
	}

	static FAInfoBarPanel()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		HorizontalOrientationPaddingProperty = AvaloniaProperty.Register<FAInfoBarPanel, Thickness>("HorizontalOrientationPadding", default(Thickness), false, (BindingMode)1, (Func<Thickness, bool>)null, (Func<AvaloniaObject, Thickness, Thickness>)null, false);
		VerticalOrientationPaddingProperty = AvaloniaProperty.Register<FAInfoBarPanel, Thickness>("VerticalOrientationPadding", default(Thickness), false, (BindingMode)1, (Func<Thickness, bool>)null, (Func<AvaloniaObject, Thickness, Thickness>)null, false);
		HorizontalOrientationMarginProperty = AvaloniaProperty.RegisterAttached<FAInfoBarPanel, Control, Thickness>("HorizontalOrientationMargin", default(Thickness), false, (BindingMode)1, (Func<Thickness, bool>)null, (Func<AvaloniaObject, Thickness, Thickness>)null);
		VerticalOrientationMarginProperty = AvaloniaProperty.RegisterAttached<FAInfoBarPanel, Control, Thickness>("VerticalOrientationMargin", default(Thickness), false, (BindingMode)1, (Func<Thickness, bool>)null, (Func<AvaloniaObject, Thickness, Thickness>)null);
	}
}
