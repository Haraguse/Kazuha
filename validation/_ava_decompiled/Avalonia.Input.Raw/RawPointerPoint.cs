using Avalonia.Metadata;

namespace Avalonia.Input.Raw;

[PrivateApi]
public record struct RawPointerPoint
{
	/// <summary>
	/// Pointer position, in client DIPs.
	/// </summary>
	public Point Position { get; set; }

	/// <inheritdoc cref="P:Avalonia.Input.PointerPointProperties.Twist" />
	public float Twist { get; set; }

	/// <inheritdoc cref="P:Avalonia.Input.PointerPointProperties.Pressure" />
	public float Pressure { get; set; }

	/// <inheritdoc cref="P:Avalonia.Input.PointerPointProperties.XTilt" />
	public float XTilt { get; set; }

	/// <inheritdoc cref="P:Avalonia.Input.PointerPointProperties.YTilt" />
	public float YTilt { get; set; }

	/// <inheritdoc cref="P:Avalonia.Input.PointerPointProperties.ContactRect" />
	public Rect ContactRect
	{
		get
		{
			return _contactRect ?? new Rect(Position, default(Size));
		}
		set
		{
			_contactRect = value;
		}
	}

	private Rect? _contactRect;

	public RawPointerPoint()
	{
		this = default(RawPointerPoint);
		Pressure = 0.5f;
	}
}
