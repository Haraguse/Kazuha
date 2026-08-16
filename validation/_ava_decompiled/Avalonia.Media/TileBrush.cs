using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media;

/// <summary>
/// Base class for brushes which display repeating images.
/// </summary>
public abstract class TileBrush : Brush, ITileBrush, IBrush
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TileBrush.AlignmentX" /> property.
	/// </summary>
	public static readonly StyledProperty<AlignmentX> AlignmentXProperty = AvaloniaProperty.Register<TileBrush, AlignmentX>("AlignmentX", AlignmentX.Center);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TileBrush.AlignmentY" /> property.
	/// </summary>
	public static readonly StyledProperty<AlignmentY> AlignmentYProperty = AvaloniaProperty.Register<TileBrush, AlignmentY>("AlignmentY", AlignmentY.Center);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TileBrush.DestinationRect" /> property.
	/// </summary>
	public static readonly StyledProperty<RelativeRect> DestinationRectProperty = AvaloniaProperty.Register<TileBrush, RelativeRect>("DestinationRect", RelativeRect.Fill);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TileBrush.SourceRect" /> property.
	/// </summary>
	public static readonly StyledProperty<RelativeRect> SourceRectProperty = AvaloniaProperty.Register<TileBrush, RelativeRect>("SourceRect", RelativeRect.Fill);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TileBrush.Stretch" /> property.
	/// </summary>
	public static readonly StyledProperty<Stretch> StretchProperty = AvaloniaProperty.Register<TileBrush, Stretch>("Stretch", Stretch.Uniform);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TileBrush.TileMode" /> property.
	/// </summary>
	public static readonly StyledProperty<TileMode> TileModeProperty = AvaloniaProperty.Register<TileBrush, TileMode>("TileMode", TileMode.None);

	/// <summary>
	/// Gets or sets the horizontal alignment of a tile in the destination.
	/// </summary>
	public AlignmentX AlignmentX
	{
		get
		{
			return GetValue(AlignmentXProperty);
		}
		set
		{
			SetValue(AlignmentXProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the horizontal alignment of a tile in the destination.
	/// </summary>
	public AlignmentY AlignmentY
	{
		get
		{
			return GetValue(AlignmentYProperty);
		}
		set
		{
			SetValue(AlignmentYProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the rectangle on the destination in which to paint a tile.
	/// </summary>
	public RelativeRect DestinationRect
	{
		get
		{
			return GetValue(DestinationRectProperty);
		}
		set
		{
			SetValue(DestinationRectProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the rectangle of the source image that will be displayed.
	/// </summary>
	public RelativeRect SourceRect
	{
		get
		{
			return GetValue(SourceRectProperty);
		}
		set
		{
			SetValue(SourceRectProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets a value controlling how the source rectangle will be stretched to fill
	/// the destination rect.
	/// </summary>
	public Stretch Stretch
	{
		get
		{
			return GetValue(StretchProperty);
		}
		set
		{
			SetValue(StretchProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the brush's tile mode.
	/// </summary>
	public TileMode TileMode
	{
		get
		{
			return GetValue(TileModeProperty);
		}
		set
		{
			SetValue(TileModeProperty, value);
		}
	}

	internal TileBrush()
	{
	}

	private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		base.SerializeChanges(c, writer);
		ServerCompositionSimpleTileBrush.SerializeAllChanges(writer, AlignmentX, AlignmentY, DestinationRect, SourceRect, Stretch, TileMode);
	}
}
