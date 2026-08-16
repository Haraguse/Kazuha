using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an icon source that uses a glyph from the SymbolThemeFontFamily resource as its content.
/// </summary>
public class FASymbolIconSource : FAIconSource
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASymbolIconSource.Symbol" /> property
	/// </summary>
	public static readonly StyledProperty<FASymbol> SymbolProperty = FASymbolIcon.SymbolProperty.AddOwner<FASymbolIconSource>((StyledPropertyMetadata<FASymbol>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FASymbolIconSource.FontSize" /> property
	/// </summary>
	public static readonly StyledProperty<double> FontSizeProperty = (StyledProperty<double>)(object)TextElement.FontSizeProperty.AddOwner<FASymbolIconSource>((StyledPropertyMetadata<double>)null);

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
}
