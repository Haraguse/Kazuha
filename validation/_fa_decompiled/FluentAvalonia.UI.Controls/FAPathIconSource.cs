using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an icon source that uses a vector path as its content.
/// </summary>
public class FAPathIconSource : FAIconSource
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAPathIconSource.Data" /> property
	/// </summary>
	public static readonly StyledProperty<Geometry> DataProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAPathIconSource.Stretch" /> property.
	/// </summary>
	public static readonly StyledProperty<Stretch> StretchProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAPathIconSource.StretchDirection" /> property.
	/// </summary>
	public static readonly StyledProperty<StretchDirection> StretchDirectionProperty;

	/// <summary>
	/// Gets or sets a Geometry that specifies the shape to be drawn. 
	/// In XAML. this can also be set using a string that describes Move and draw commands syntax.
	/// </summary>
	public Geometry Data
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Geometry>(DataProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Geometry>(DataProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a <see cref="P:FluentAvalonia.UI.Controls.FAPathIconSource.Stretch" /> enumeration value that describes how the shape fills its allocated space.
	/// </summary>
	public Stretch Stretch
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<Stretch>(StretchProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<Stretch>(StretchProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value controlling in what direction contents will be stretched.
	/// </summary>
	public StretchDirection StretchDirection
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<StretchDirection>(StretchDirectionProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<StretchDirection>(StretchDirectionProperty, value, (BindingPriority)0);
		}
	}

	static FAPathIconSource()
	{
		DataProperty = FAPathIcon.DataProperty.AddOwner<FAPathIconSource>((StyledPropertyMetadata<Geometry>)null);
		StretchProperty = FAPathIcon.StretchProperty.AddOwner<FAPathIcon>((StyledPropertyMetadata<Stretch>)null);
		StretchDirectionProperty = FAPathIcon.StretchDirectionProperty.AddOwner<PathIcon>((StyledPropertyMetadata<StretchDirection>)null);
		StretchProperty.OverrideDefaultValue<FAPathIconSource>((Stretch)2);
		StretchDirectionProperty.OverrideDefaultValue<FAPathIconSource>((StretchDirection)2);
	}
}
