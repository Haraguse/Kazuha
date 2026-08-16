using System;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// Paints an area with an <see cref="T:Avalonia.Media.Imaging.IBitmap" />.
/// </summary>
public sealed class ImageBrush : TileBrush, IImageBrush, ITileBrush, IBrush, IMutableBrush
{
	/// <summary>
	/// Defines the <see cref="T:Avalonia.Visual" /> property.
	/// </summary>
	public static readonly StyledProperty<IImageBrushSource?> SourceProperty = AvaloniaProperty.Register<ImageBrush, IImageBrushSource>("Source");

	/// <summary>
	/// Gets or sets the image to draw.
	/// </summary>
	public IImageBrushSource? Source
	{
		get
		{
			return GetValue(SourceProperty);
		}
		set
		{
			SetValue(SourceProperty, value);
		}
	}

	internal override Func<Compositor, ServerCompositionSimpleBrush> Factory => (Compositor c) => new ServerCompositionSimpleImageBrush(c.Server);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.ImageBrush" /> class.
	/// </summary>
	public ImageBrush()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.ImageBrush" /> class.
	/// </summary>
	/// <param name="source">The image to draw.</param>
	public ImageBrush(IImageBrushSource? source)
	{
		Source = source;
	}

	/// <inheritdoc />
	public IImmutableBrush ToImmutable()
	{
		return new ImmutableImageBrush(this);
	}

	private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		base.SerializeChanges(c, writer);
		IRef<IBitmapImpl> item = Source?.Bitmap?.Clone();
		writer.WriteObject(item);
	}
}
