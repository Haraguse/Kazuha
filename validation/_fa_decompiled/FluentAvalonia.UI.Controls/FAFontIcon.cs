using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an icon that uses a glyph from the specified font.
/// </summary>
public class FAFontIcon : FAIconElement
{
	private TextLayout _textLayout;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFontIcon.FontFamily" /> property
	/// </summary>
	public static readonly StyledProperty<FontFamily> FontFamilyProperty = (StyledProperty<FontFamily>)(object)TextElement.FontFamilyProperty.AddOwner<FAFontIcon>((StyledPropertyMetadata<FontFamily>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFontIcon.FontSize" /> property
	/// </summary>
	public static readonly StyledProperty<double> FontSizeProperty = (StyledProperty<double>)(object)TextElement.FontSizeProperty.AddOwner<FAFontIcon>((StyledPropertyMetadata<double>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFontIcon.FontWeight" /> property
	/// </summary>
	public static readonly StyledProperty<FontWeight> FontWeightProperty = (StyledProperty<FontWeight>)(object)TextElement.FontWeightProperty.AddOwner<FAFontIcon>((StyledPropertyMetadata<FontWeight>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFontIcon.FontStyle" /> property
	/// </summary>
	public static readonly StyledProperty<FontStyle> FontStyleProperty = (StyledProperty<FontStyle>)(object)TextElement.FontStyleProperty.AddOwner<FAFontIcon>((StyledPropertyMetadata<FontStyle>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFontIcon.Glyph" /> property
	/// </summary>
	public static readonly StyledProperty<string> GlyphProperty = AvaloniaProperty.Register<FAFontIcon, string>("Glyph", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null, false);

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Media.FontFamily" /> to use when rendering
	/// the glyph
	/// </summary>
	public FontFamily FontFamily
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FontFamily>(FontFamilyProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FontFamily>(FontFamilyProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the font size to use when rendering the glyph
	/// </summary>
	public double FontSize
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<double>(FontSizeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<double>(FontSizeProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Media.FontWeight" /> to use 
	/// when rendering the glyph
	/// </summary>
	public FontWeight FontWeight
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<FontWeight>(FontWeightProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<FontWeight>(FontWeightProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Media.FontStyle" /> to use 
	/// when rendering the glyph
	/// </summary>
	public FontStyle FontStyle
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<FontStyle>(FontStyleProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<FontStyle>(FontStyleProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the glyph this FontIcon renders
	/// </summary>
	public string Glyph
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<string>(GlyphProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<string>(GlyphProperty, value, (BindingPriority)0);
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)TextElement.FontSizeProperty || change.Property == (AvaloniaProperty)(object)TextElement.FontFamilyProperty || change.Property == (AvaloniaProperty)(object)TextElement.FontWeightProperty || change.Property == (AvaloniaProperty)(object)TextElement.FontStyleProperty || change.Property == (AvaloniaProperty)(object)GlyphProperty)
		{
			_textLayout = null;
			((Layoutable)this).InvalidateMeasure();
		}
		else if (change.Property == (AvaloniaProperty)(object)TextElement.ForegroundProperty)
		{
			_textLayout = null;
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (_textLayout == null)
		{
			GenerateText();
		}
		return new Size(_textLayout.Width, _textLayout.Height);
	}

	public unsafe override void Render(DrawingContext context)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (_textLayout == null)
		{
			GenerateText();
		}
		Rect bounds = ((Visual)this).Bounds;
		Rect val = default(Rect);
		((Rect)(ref val))._002Ector(((Rect)(ref bounds)).Size);
		PushedState val2 = context.PushClip(val);
		try
		{
			Point center = ((Rect)(ref val)).Center;
			double num = ((Point)(ref center)).X - _textLayout.Width * 0.5;
			center = ((Rect)(ref val)).Center;
			Point val3 = default(Point);
			((Point)(ref val3))._002Ector(num, ((Point)(ref center)).Y - _textLayout.Height * 0.5);
			_textLayout.Draw(context, val3);
		}
		finally
		{
			((IDisposable)(*(PushedState*)(&val2))/*cast due to constrained. prefix*/).Dispose();
		}
	}

	private void GenerateText()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		_textLayout = new TextLayout(Glyph, new Typeface(FontFamily, FontStyle, FontWeight, (FontStretch)5), FontSize, base.Foreground, (TextAlignment)0, (TextWrapping)0, (TextTrimming)null, (TextDecorationCollection)null, (FlowDirection)0, double.PositiveInfinity, double.PositiveInfinity, double.NaN, 0.0, 0, (FontFeatureCollection)null, (IReadOnlyList<ValueSpan<TextRunProperties>>)null);
	}
}
