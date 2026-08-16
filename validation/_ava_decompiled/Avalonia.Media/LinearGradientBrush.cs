using System;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media;

/// <summary>
/// A brush that draws with a linear gradient.
/// </summary>
public sealed class LinearGradientBrush : GradientBrush, ILinearGradientBrush, IGradientBrush, IBrush
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.LinearGradientBrush.StartPoint" /> property.
	/// </summary>
	public static readonly StyledProperty<RelativePoint> StartPointProperty = AvaloniaProperty.Register<LinearGradientBrush, RelativePoint>("StartPoint", RelativePoint.TopLeft);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.LinearGradientBrush.EndPoint" /> property.
	/// </summary>
	public static readonly StyledProperty<RelativePoint> EndPointProperty = AvaloniaProperty.Register<LinearGradientBrush, RelativePoint>("EndPoint", RelativePoint.BottomRight);

	/// <summary>
	/// Gets or sets the start point for the gradient.
	/// </summary>
	public RelativePoint StartPoint
	{
		get
		{
			return GetValue(StartPointProperty);
		}
		set
		{
			SetValue(StartPointProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the end point for the gradient.
	/// </summary>
	public RelativePoint EndPoint
	{
		get
		{
			return GetValue(EndPointProperty);
		}
		set
		{
			SetValue(EndPointProperty, value);
		}
	}

	internal override Func<Compositor, ServerCompositionSimpleBrush> Factory => (Compositor c) => new ServerCompositionSimpleLinearGradientBrush(c.Server);

	/// <inheritdoc />
	public override IImmutableBrush ToImmutable()
	{
		return new ImmutableLinearGradientBrush(this);
	}

	private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		base.SerializeChanges(c, writer);
		ServerCompositionSimpleLinearGradientBrush.SerializeAllChanges(writer, StartPoint, EndPoint);
	}
}
