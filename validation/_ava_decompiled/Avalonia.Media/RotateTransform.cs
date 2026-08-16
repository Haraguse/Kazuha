using Avalonia.Reactive;

namespace Avalonia.Media;

/// <summary>
/// Rotates a <see cref="T:Avalonia.Visual" />.
/// </summary>
public sealed class RotateTransform : Transform
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.RotateTransform.Angle" /> property.
	/// </summary>
	public static readonly StyledProperty<double> AngleProperty = AvaloniaProperty.Register<RotateTransform, double>("Angle", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.RotateTransform.CenterX" /> property.
	/// </summary>
	public static readonly StyledProperty<double> CenterXProperty = AvaloniaProperty.Register<RotateTransform, double>("CenterX", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.RotateTransform.CenterY" /> property.
	/// </summary>
	public static readonly StyledProperty<double> CenterYProperty = AvaloniaProperty.Register<RotateTransform, double>("CenterY", 0.0);

	/// <summary>
	/// Gets or sets the angle of rotation, in degrees.
	/// </summary>
	public double Angle
	{
		get
		{
			return GetValue(AngleProperty);
		}
		set
		{
			SetValue(AngleProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the x-coordinate of the rotation center point. The default is 0 which is the <see cref="P:Avalonia.Visual.RenderTransformOrigin" /> point (center by default).
	/// </summary>
	public double CenterX
	{
		get
		{
			return GetValue(CenterXProperty);
		}
		set
		{
			SetValue(CenterXProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the y-coordinate of the rotation center point. The default is 0 which is the <see cref="P:Avalonia.Visual.RenderTransformOrigin" /> point (center by default).
	/// </summary>
	public double CenterY
	{
		get
		{
			return GetValue(CenterYProperty);
		}
		set
		{
			SetValue(CenterYProperty, value);
		}
	}

	/// <summary>
	/// Gets the transform's <see cref="T:Avalonia.Matrix" />.
	/// </summary>
	public override Matrix Value => Matrix.CreateTranslation(0.0 - CenterX, 0.0 - CenterY) * Matrix.CreateRotation(Matrix.ToRadians(Angle)) * Matrix.CreateTranslation(CenterX, CenterY);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.RotateTransform" /> class.
	/// </summary>
	public RotateTransform()
	{
		this.GetObservable(AngleProperty).Subscribe(delegate
		{
			RaiseChanged();
		});
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.RotateTransform" /> class.
	/// </summary>
	/// <param name="angle">The angle, in degrees.</param>
	public RotateTransform(double angle)
		: this()
	{
		Angle = angle;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.RotateTransform" /> class.
	/// </summary>
	/// <param name="angle">The angle, in degrees.</param>
	/// <param name="centerX">The x-coordinate of the center point for the rotation with 0 being the <see cref="P:Avalonia.Visual.RenderTransformOrigin" /> point (center by default).</param>
	/// <param name="centerY">The y-coordinate of the center point for the rotation with 0 being the <see cref="P:Avalonia.Visual.RenderTransformOrigin" /> point (center by default).</param>
	public RotateTransform(double angle, double centerX, double centerY)
		: this()
	{
		Angle = angle;
		CenterX = centerX;
		CenterY = centerY;
	}
}
