using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Metadata;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an icon source that uses an image type as its content.
/// </summary>
public class FAImageIconSource : FAIconSource
{
	/// <summary>
	/// Gets or sets the <see cref="P:FluentAvalonia.UI.Controls.FAImageIconSource.Source" /> property
	/// </summary>
	public static readonly StyledProperty<IImage> SourceProperty = FAImageIcon.SourceProperty.AddOwner<FAImageIconSource>((StyledPropertyMetadata<IImage>)null);

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Media.IImage" /> content this icon displays
	/// </summary>
	[Content]
	public IImage Source
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IImage>(SourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IImage>(SourceProperty, value, (BindingPriority)0);
		}
	}
}
