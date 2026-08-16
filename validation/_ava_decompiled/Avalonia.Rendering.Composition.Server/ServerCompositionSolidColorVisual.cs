using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionSolidColorVisual : ServerSizeDependantVisual
{
	private Color _color;

	internal static readonly CompositionProperty<Color> s_IdOfColorProperty = CompositionProperty.Register<ServerCompositionSolidColorVisual, Color>("Color", (SimpleServerObject obj) => ((ServerCompositionSolidColorVisual)obj)._color, delegate(SimpleServerObject obj, Color v)
	{
		((ServerCompositionSolidColorVisual)obj)._color = v;
	}, (SimpleServerObject obj) => ((ServerCompositionSolidColorVisual)obj)._color);

	public Color Color
	{
		get
		{
			return _color;
		}
		set
		{
			SetAnimatedValue(s_IdOfColorProperty, out _color, value);
		}
	}

	protected override void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
	{
		context.Canvas.DrawRectangle(new ImmutableSolidColorBrush(Color), null, new Rect(0.0, 0.0, base.Size.X, base.Size.Y));
	}

	internal ServerCompositionSolidColorVisual(ServerCompositor compositor)
		: base(compositor)
	{
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		CompositionSolidColorVisualChangedFields compositionSolidColorVisualChangedFields = reader.Read<CompositionSolidColorVisualChangedFields>();
		if ((compositionSolidColorVisualChangedFields & CompositionSolidColorVisualChangedFields.ColorAnimated) == CompositionSolidColorVisualChangedFields.ColorAnimated)
		{
			SetAnimatedValue(s_IdOfColorProperty, ref _color, committedAt, reader.ReadObject<IAnimationInstance>());
		}
		else if ((compositionSolidColorVisualChangedFields & CompositionSolidColorVisualChangedFields.Color) == CompositionSolidColorVisualChangedFields.Color)
		{
			Color = reader.Read<Color>();
		}
	}

	public override CompositionProperty? GetCompositionProperty(string name)
	{
		if (name == "Color")
		{
			return s_IdOfColorProperty;
		}
		return base.GetCompositionProperty(name);
	}
}
