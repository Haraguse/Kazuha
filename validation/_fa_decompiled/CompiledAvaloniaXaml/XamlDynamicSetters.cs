using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Controls.Primitives;

namespace CompiledAvaloniaXaml;

[CompilerGenerated]
internal class XamlDynamicSetters
{
	public static void _003C_003EXamlDynamicSetter_1(TextBox P_0, BindingPriority P_1, BindingBase P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		if (P_2 != null)
		{
			BindingBase val = P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)TextBox.InnerLeftContentProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>(TextBox.InnerLeftContentProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_2(TextBox P_0, BindingPriority P_1, BindingBase P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		if (P_2 != null)
		{
			BindingBase val = P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)TextBox.InnerRightContentProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>(TextBox.InnerRightContentProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_3(ContentPresenter P_0, BindingPriority P_1, BindingBase P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		if (P_2 != null)
		{
			BindingBase val = P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)ContentPresenter.ContentProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>(ContentPresenter.ContentProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_4(StyledElement P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)StyledElement.ThemeProperty, val);
			return;
		}
		if (P_2 is ControlTheme)
		{
			ControlTheme val2 = (ControlTheme)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<ControlTheme>(StyledElement.ThemeProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			ControlTheme val2 = (ControlTheme)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<ControlTheme>(StyledElement.ThemeProperty, val2, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_5(ContentControl P_0, BindingPriority P_1, BindingBase P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		if (P_2 != null)
		{
			BindingBase val = P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)ContentControl.ContentProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>(ContentControl.ContentProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_6(StyledElement P_0, object P_1)
	{
		if (P_1 is UnsetValueType)
		{
			((AvaloniaObject)P_0).SetValue((AvaloniaProperty)(object)StyledElement.ThemeProperty, AvaloniaProperty.UnsetValue, (BindingPriority)0);
			return;
		}
		if (P_1 is BindingBase)
		{
			BindingBase val = (BindingBase)P_1;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)StyledElement.ThemeProperty, val);
			return;
		}
		if (P_1 is ControlTheme)
		{
			P_0.Theme = (ControlTheme)P_1;
			return;
		}
		if (P_1 == null)
		{
			P_0.Theme = (ControlTheme)P_1;
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_7(Layoutable P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Layoutable.HeightProperty, val);
			return;
		}
		if (P_2 is double num)
		{
			int num2 = (int)P_1;
			((AvaloniaObject)P_0).SetValue<double>(Layoutable.HeightProperty, num, (BindingPriority)num2);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_8(Layoutable P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Layoutable.WidthProperty, val);
			return;
		}
		if (P_2 is double num)
		{
			int num2 = (int)P_1;
			((AvaloniaObject)P_0).SetValue<double>(Layoutable.WidthProperty, num, (BindingPriority)num2);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_9(FAFontIcon P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FAFontIcon.FontFamilyProperty, val);
			return;
		}
		if (P_2 is FontFamily)
		{
			FontFamily val2 = (FontFamily)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<FontFamily>(FAFontIcon.FontFamilyProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			FontFamily val2 = (FontFamily)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<FontFamily>(FAFontIcon.FontFamilyProperty, val2, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_10(FAFontIcon P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FAFontIcon.FontSizeProperty, val);
			return;
		}
		if (P_2 is double num)
		{
			int num2 = (int)P_1;
			((AvaloniaObject)P_0).SetValue<double>(FAFontIcon.FontSizeProperty, num, (BindingPriority)num2);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_11(Border P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Border.BackgroundProperty, val);
			return;
		}
		if (P_2 is IBrush)
		{
			IBrush val2 = (IBrush)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<IBrush>(Border.BackgroundProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			IBrush val2 = (IBrush)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<IBrush>(Border.BackgroundProperty, val2, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_12(ItemsControl P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)ItemsControl.ItemContainerThemeProperty, val);
			return;
		}
		if (P_2 is ControlTheme)
		{
			ControlTheme val2 = (ControlTheme)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<ControlTheme>(ItemsControl.ItemContainerThemeProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			ControlTheme val2 = (ControlTheme)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<ControlTheme>(ItemsControl.ItemContainerThemeProperty, val2, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_13(SelectingItemsControl P_0, CompiledBinding P_1)
	{
		if (P_1 != null)
		{
			BindingBase val = (BindingBase)(object)P_1;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)SelectingItemsControl.SelectedItemProperty, val);
		}
		else
		{
			P_0.SelectedItem = P_1;
		}
	}

	public static void _003C_003EXamlDynamicSetter_14(ToolTip P_0, BindingPriority P_1, CompiledBinding P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		if (P_2 != null)
		{
			BindingBase val = (BindingBase)(object)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)ToolTip.TipProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>((StyledProperty<object>)(object)ToolTip.TipProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_15(NumericUpDown P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)NumericUpDown.NumberFormatProperty, val);
			return;
		}
		if (P_2 is NumberFormatInfo)
		{
			NumberFormatInfo numberFormatInfo = (NumberFormatInfo)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<NumberFormatInfo>(NumericUpDown.NumberFormatProperty, numberFormatInfo, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			NumberFormatInfo numberFormatInfo = (NumberFormatInfo)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<NumberFormatInfo>(NumericUpDown.NumberFormatProperty, numberFormatInfo, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_16(StyledElement P_0, BindingPriority P_1, BindingBase P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		if (P_2 != null)
		{
			BindingBase val = P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)StyledElement.DataContextProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>(StyledElement.DataContextProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_17(Popup P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Popup.PlacementTargetProperty, val);
			return;
		}
		if (P_2 is Control)
		{
			Control val2 = (Control)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<Control>(Popup.PlacementTargetProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			Control val2 = (Control)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<Control>(Popup.PlacementTargetProperty, val2, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_18(ContentControl P_0, BindingPriority P_1, CompiledBinding P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		if (P_2 != null)
		{
			BindingBase val = (BindingBase)(object)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)ContentControl.ContentProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>(ContentControl.ContentProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_19(Layoutable P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Layoutable.MarginProperty, val);
			return;
		}
		if (P_2 is Thickness val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<Thickness>(Layoutable.MarginProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_20(ContentPresenter P_0, BindingPriority P_1, CompiledBinding P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		if (P_2 != null)
		{
			BindingBase val = (BindingBase)(object)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)ContentPresenter.ContentProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>(ContentPresenter.ContentProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_21(FAIconSourceElement P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FAIconSourceElement.IconSourceProperty, val);
			return;
		}
		if (P_2 is FAIconSource)
		{
			FAIconSource fAIconSource = (FAIconSource)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<FAIconSource>(FAIconSourceElement.IconSourceProperty, fAIconSource, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			FAIconSource fAIconSource = (FAIconSource)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<FAIconSource>(FAIconSourceElement.IconSourceProperty, fAIconSource, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_22(Layoutable P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Layoutable.MinHeightProperty, val);
			return;
		}
		if (P_2 is double num)
		{
			int num2 = (int)P_1;
			((AvaloniaObject)P_0).SetValue<double>(Layoutable.MinHeightProperty, num, (BindingPriority)num2);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_23(Button P_0, BindingPriority P_1, BindingBase P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		if (P_2 != null)
		{
			BindingBase val = P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Button.CommandParameterProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>(Button.CommandParameterProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_24(TransitionBase P_0, object P_1)
	{
		if (P_1 is UnsetValueType)
		{
			((AvaloniaObject)P_0).SetValue((AvaloniaProperty)(object)TransitionBase.DurationProperty, AvaloniaProperty.UnsetValue, (BindingPriority)0);
			return;
		}
		if (P_1 is BindingBase)
		{
			BindingBase val = (BindingBase)P_1;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)TransitionBase.DurationProperty, val);
			return;
		}
		if (P_1 is TimeSpan)
		{
			P_0.Duration = (TimeSpan)P_1;
			return;
		}
		if (P_1 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_25(FASymbolIcon P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FASymbolIcon.FontSizeProperty, val);
			return;
		}
		if (P_2 is double num)
		{
			int num2 = (int)P_1;
			((AvaloniaObject)P_0).SetValue<double>(FASymbolIcon.FontSizeProperty, num, (BindingPriority)num2);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_26(TemplatedControl P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)TemplatedControl.PaddingProperty, val);
			return;
		}
		if (P_2 is Thickness val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<Thickness>(TemplatedControl.PaddingProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_27(Border P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Border.BorderThicknessProperty, val);
			return;
		}
		if (P_2 is Thickness val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<Thickness>(Border.BorderThicknessProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_28(ContentPresenter P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)ContentPresenter.FontSizeProperty, val);
			return;
		}
		if (P_2 is double num)
		{
			int num2 = (int)P_1;
			((AvaloniaObject)P_0).SetValue<double>(ContentPresenter.FontSizeProperty, num, (BindingPriority)num2);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_29(ContentPresenter P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)ContentPresenter.FontFamilyProperty, val);
			return;
		}
		if (P_2 is FontFamily)
		{
			FontFamily val2 = (FontFamily)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<FontFamily>(ContentPresenter.FontFamilyProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			FontFamily val2 = (FontFamily)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<FontFamily>(ContentPresenter.FontFamilyProperty, val2, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_30(Decorator P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Decorator.PaddingProperty, val);
			return;
		}
		if (P_2 is Thickness val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<Thickness>(Decorator.PaddingProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_31(FAFontIconSource P_0, object P_1)
	{
		if (P_1 is UnsetValueType)
		{
			((AvaloniaObject)P_0).SetValue((AvaloniaProperty)(object)FAFontIconSource.FontFamilyProperty, AvaloniaProperty.UnsetValue, (BindingPriority)0);
			return;
		}
		if (P_1 is BindingBase)
		{
			BindingBase val = (BindingBase)P_1;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FAFontIconSource.FontFamilyProperty, val);
			return;
		}
		if (P_1 is FontFamily)
		{
			P_0.FontFamily = (FontFamily)P_1;
			return;
		}
		if (P_1 == null)
		{
			P_0.FontFamily = (FontFamily)P_1;
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_32(FAInfoBarPanel P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FAInfoBarPanel.HorizontalOrientationPaddingProperty, val);
			return;
		}
		if (P_2 is Thickness val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<Thickness>(FAInfoBarPanel.HorizontalOrientationPaddingProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_33(FAInfoBarPanel P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FAInfoBarPanel.VerticalOrientationPaddingProperty, val);
			return;
		}
		if (P_2 is Thickness val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<Thickness>(FAInfoBarPanel.VerticalOrientationPaddingProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_34(FAInfoBarPanel P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FAInfoBarPanel.HorizontalOrientationMarginProperty, val);
			return;
		}
		if (P_2 is Thickness val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<Thickness>((StyledProperty<Thickness>)(object)FAInfoBarPanel.HorizontalOrientationMarginProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_35(FAInfoBarPanel P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FAInfoBarPanel.VerticalOrientationMarginProperty, val);
			return;
		}
		if (P_2 is Thickness val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<Thickness>((StyledProperty<Thickness>)(object)FAInfoBarPanel.VerticalOrientationMarginProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_36(TextBlock P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)TextBlock.FontWeightProperty, val);
			return;
		}
		if (P_2 is FontWeight val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<FontWeight>(TextBlock.FontWeightProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_37(TextBlock P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)TextBlock.FontSizeProperty, val);
			return;
		}
		if (P_2 is double num)
		{
			int num2 = (int)P_1;
			((AvaloniaObject)P_0).SetValue<double>(TextBlock.FontSizeProperty, num, (BindingPriority)num2);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_38(FASymbolIcon P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FASymbolIcon.SymbolProperty, val);
			return;
		}
		if (P_2 is FASymbol fASymbol)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<FASymbol>(FASymbolIcon.SymbolProperty, fASymbol, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_39(ContentControl P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)ContentControl.ContentProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>(ContentControl.ContentProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_40(TextBlock P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)TextBlock.ForegroundProperty, val);
			return;
		}
		if (P_2 is IBrush)
		{
			IBrush val2 = (IBrush)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<IBrush>(TextBlock.ForegroundProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			IBrush val2 = (IBrush)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<IBrush>(TextBlock.ForegroundProperty, val2, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_41(TextBlock P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)TextBlock.TextProperty, val);
			return;
		}
		if (P_2 is string)
		{
			string text = (string)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<string>(TextBlock.TextProperty, text, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			string text = (string)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<string>(TextBlock.TextProperty, text, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_42(TextBlock P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		//IL_0040: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)TextBlock.FontFamilyProperty, val);
			return;
		}
		if (P_2 is FontFamily)
		{
			FontFamily val2 = (FontFamily)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<FontFamily>(TextBlock.FontFamilyProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			FontFamily val2 = (FontFamily)P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<FontFamily>(TextBlock.FontFamilyProperty, val2, (BindingPriority)num);
			return;
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_43(ColumnDefinition P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)ColumnDefinition.WidthProperty, val);
			return;
		}
		if (P_2 is GridLength val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<GridLength>(ColumnDefinition.WidthProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_44(RowDefinition P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)RowDefinition.HeightProperty, val);
			return;
		}
		if (P_2 is GridLength val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<GridLength>(RowDefinition.HeightProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_45(Shape P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Shape.StrokeThicknessProperty, val);
			return;
		}
		if (P_2 is double num)
		{
			int num2 = (int)P_1;
			((AvaloniaObject)P_0).SetValue<double>(Shape.StrokeThicknessProperty, num, (BindingPriority)num2);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_46(FASettingsExpanderItem P_0, BindingPriority P_1, BindingBase P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected I4, but got Unknown
		if (P_2 != null)
		{
			BindingBase val = P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)FASettingsExpanderItem.FooterProperty, val);
		}
		else
		{
			object obj = P_2;
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<object>(FASettingsExpanderItem.FooterProperty, obj, (BindingPriority)num);
		}
	}

	public static void _003C_003EXamlDynamicSetter_47(Border P_0, BindingPriority P_1, object P_2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected I4, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (P_2 is BindingBase)
		{
			BindingBase val = (BindingBase)P_2;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)Border.CornerRadiusProperty, val);
			return;
		}
		if (P_2 is CornerRadius val2)
		{
			int num = (int)P_1;
			((AvaloniaObject)P_0).SetValue<CornerRadius>(Border.CornerRadiusProperty, val2, (BindingPriority)num);
			return;
		}
		if (P_2 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_48(SolidColorBrush P_0, object P_1)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (P_1 is UnsetValueType)
		{
			((AvaloniaObject)P_0).SetValue((AvaloniaProperty)(object)SolidColorBrush.ColorProperty, AvaloniaProperty.UnsetValue, (BindingPriority)0);
			return;
		}
		if (P_1 is BindingBase)
		{
			BindingBase val = (BindingBase)P_1;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)SolidColorBrush.ColorProperty, val);
			return;
		}
		if (P_1 is Color)
		{
			P_0.Color = (Color)P_1;
			return;
		}
		if (P_1 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}

	public static void _003C_003EXamlDynamicSetter_49(GradientStop P_0, object P_1)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (P_1 is UnsetValueType)
		{
			((AvaloniaObject)P_0).SetValue((AvaloniaProperty)(object)GradientStop.ColorProperty, AvaloniaProperty.UnsetValue, (BindingPriority)0);
			return;
		}
		if (P_1 is BindingBase)
		{
			BindingBase val = (BindingBase)P_1;
			((AvaloniaObject)P_0).Bind((AvaloniaProperty)(object)GradientStop.ColorProperty, val);
			return;
		}
		if (P_1 is Color)
		{
			P_0.Color = (Color)P_1;
			return;
		}
		if (P_1 == null)
		{
			throw new NullReferenceException();
		}
		throw new InvalidCastException();
	}
}
