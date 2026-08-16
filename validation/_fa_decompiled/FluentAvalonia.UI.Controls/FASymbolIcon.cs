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
/// Represents an icon that uses a glyph from the SymbolThemeFontFamily resource as its content.
/// </summary>
public class FASymbolIcon : FAIconElement
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASymbolIcon.Symbol" /> property
	/// </summary>
	public static readonly StyledProperty<FASymbol> SymbolProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASymbolIcon.FontSize" /> property
	/// </summary>
	public static readonly StyledProperty<double> FontSizeProperty;

	private TextLayout _textLayout;

	private static FontFamily _symbolFontFamily;

	/// <summary>
	/// Gets or sets the <see cref="T:FluentAvalonia.UI.Controls.FASymbol" /> this icon displays
	/// </summary>
	public FASymbol Symbol
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FASymbol>(SymbolProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FASymbol>(SymbolProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets the font size this icon uses when rendering
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

	static FASymbolIcon()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		SymbolProperty = AvaloniaProperty.Register<FASymbolIcon, FASymbol>("Symbol", (FASymbol)0, false, (BindingMode)1, (Func<FASymbol, bool>)null, (Func<AvaloniaObject, FASymbol, FASymbol>)null, false);
		FontSizeProperty = (StyledProperty<double>)(object)TextElement.FontSizeProperty.AddOwner<FASymbolIcon>((StyledPropertyMetadata<double>)null);
		FontSizeProperty.OverrideDefaultValue<FASymbolIcon>(18.0);
		_symbolFontFamily = new FontFamily("avares://FluentAvalonia/Fonts#Symbols");
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)TextElement.FontSizeProperty || change.Property == (AvaloniaProperty)(object)SymbolProperty)
		{
			_textLayout = null;
			((Layoutable)this).InvalidateMeasure();
		}
		else if (change.Property == (AvaloniaProperty)(object)TextElement.ForegroundProperty)
		{
			_textLayout = null;
		}
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		((Visual)this).OnAttachedToVisualTree(e);
		if (_textLayout != null)
		{
			GenerateText();
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
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		string text = char.ConvertFromUtf32((int)Symbol).ToString();
		_textLayout = new TextLayout(text, new Typeface(_symbolFontFamily, (FontStyle)0, (FontWeight)400, (FontStretch)5), FontSize, base.Foreground, (TextAlignment)0, (TextWrapping)0, (TextTrimming)null, (TextDecorationCollection)null, (FlowDirection)0, double.PositiveInfinity, double.PositiveInfinity, double.NaN, 0.0, 0, (FontFeatureCollection)null, (IReadOnlyList<ValueSpan<TextRunProperties>>)null);
	}
}
