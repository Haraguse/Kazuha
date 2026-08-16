namespace Avalonia.Media;

public abstract class PathSegment : AvaloniaObject
{
	public static readonly StyledProperty<bool> IsStrokedProperty = AvaloniaProperty.Register<PathSegment, bool>("IsStroked", defaultValue: true);

	public bool IsStroked
	{
		get
		{
			return GetValue(IsStrokedProperty);
		}
		set
		{
			SetValue(IsStrokedProperty, value);
		}
	}

	internal abstract void ApplyTo(StreamGeometryContext ctx);
}
