using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an icon source that uses a glyph from the specified font.
/// </summary>
public class FAFontIconSource : FAIconSource
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFontIconSource.FontFamily" /> property
	/// </summary>
	public static readonly StyledProperty<FontFamily> FontFamilyProperty = (StyledProperty<FontFamily>)(object)TextElement.FontFamilyProperty.AddOwner<FAFontIconSource>((StyledPropertyMetadata<FontFamily>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFontIconSource.FontSize" /> property
	/// </summary>
	public static readonly StyledProperty<double> FontSizeProperty = (StyledProperty<double>)(object)TextElement.FontSizeProperty.AddOwner<FAFontIconSource>((StyledPropertyMetadata<double>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFontIconSource.FontWeight" /> property
	/// </summary>
	public static readonly StyledProperty<FontWeight> FontWeightProperty = (StyledProperty<FontWeight>)(object)TextElement.FontWeightProperty.AddOwner<FAFontIconSource>((StyledPropertyMetadata<FontWeight>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFontIconSource.FontStyle" /> property
	/// </summary>
	public static readonly StyledProperty<FontStyle> FontStyleProperty = (StyledProperty<FontStyle>)(object)TextElement.FontStyleProperty.AddOwner<FAFontIconSource>((StyledPropertyMetadata<FontStyle>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAFontIconSource.Glyph" /> property
	/// </summary>
	public static readonly StyledProperty<string> GlyphProperty = FAFontIcon.GlyphProperty.AddOwner<FAFontIconSource>((StyledPropertyMetadata<string>)null);

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
}
