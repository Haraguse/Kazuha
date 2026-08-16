using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the base class for an icon UI element.
/// </summary>
[TypeConverter(typeof(IconElementConverter))]
public class FAIconElement : Control
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAIconElement.Foreground" /> property
	/// </summary>
	public static readonly AttachedProperty<IBrush> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<FAIconElement>((StyledPropertyMetadata<IBrush>)null);

	/// <summary>
	/// Gets or sets a brush that describes the foreground color.
	/// </summary>
	public IBrush Foreground
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IBrush>((StyledProperty<IBrush>)(object)ForegroundProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IBrush>((StyledProperty<IBrush>)(object)ForegroundProperty, value, (BindingPriority)0);
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((Control)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)ForegroundProperty)
		{
			((Visual)this).InvalidateVisual();
		}
	}
}
