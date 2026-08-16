using System;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media;

/// <summary>
/// Fills an area with a solid color.
/// </summary>
public sealed class SolidColorBrush : Brush, ISolidColorBrush, IBrush, IMutableBrush
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.SolidColorBrush.Color" /> property.
	/// </summary>
	public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<SolidColorBrush, Color>("Color");

	/// <summary>
	/// Gets or sets the color of the brush.
	/// </summary>
	public Color Color
	{
		get
		{
			return GetValue(ColorProperty);
		}
		set
		{
			SetValue(ColorProperty, value);
		}
	}

	internal override Func<Compositor, ServerCompositionSimpleBrush> Factory => (Compositor c) => new ServerCompositionSimpleSolidColorBrush(c.Server);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.SolidColorBrush" /> class.
	/// </summary>
	public SolidColorBrush()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.SolidColorBrush" /> class.
	/// </summary>
	/// <param name="color">The color to use.</param>
	/// <param name="opacity">The opacity of the brush.</param>
	public SolidColorBrush(Color color, double opacity = 1.0)
	{
		Color = color;
		base.Opacity = opacity;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.SolidColorBrush" /> class.
	/// </summary>
	/// <param name="color">The color to use.</param>
	public SolidColorBrush(uint color)
		: this(Color.FromUInt32(color))
	{
	}

	/// <summary>
	/// Parses a brush string.
	/// </summary>
	/// <param name="s">The brush string.</param>
	/// <returns>The <see cref="P:Avalonia.Media.SolidColorBrush.Color" />.</returns>
	/// <remarks>
	/// Whereas <see cref="M:Avalonia.Media.Brush.Parse(System.String)" /> may return an immutable solid color brush,
	/// this method always returns a mutable <see cref="T:Avalonia.Media.SolidColorBrush" />.
	/// </remarks>
	public new static SolidColorBrush Parse(string s)
	{
		ISolidColorBrush solidColorBrush = (ISolidColorBrush)Brush.Parse(s);
		if (!(solidColorBrush is SolidColorBrush result))
		{
			return new SolidColorBrush(solidColorBrush.Color);
		}
		return result;
	}

	/// <summary>
	/// Returns a string representation of the brush.
	/// </summary>
	/// <returns>A string representation of the brush.</returns>
	public override string ToString()
	{
		return Color.ToString();
	}

	/// <inheritdoc />
	public IImmutableBrush ToImmutable()
	{
		return new ImmutableSolidColorBrush(this);
	}

	private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		base.SerializeChanges(c, writer);
		ServerCompositionSimpleSolidColorBrush.SerializeAllChanges(writer, Color);
	}
}
