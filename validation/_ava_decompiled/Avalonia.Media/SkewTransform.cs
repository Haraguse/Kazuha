namespace Avalonia.Media;

/// <summary>
/// Skews an <see cref="T:Avalonia.Visual" />.
/// </summary>
public sealed class SkewTransform : Transform
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.SkewTransform.AngleX" /> property.
	/// </summary>
	public static readonly StyledProperty<double> AngleXProperty = AvaloniaProperty.Register<SkewTransform, double>("AngleX", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.SkewTransform.AngleY" /> property.
	/// </summary>
	public static readonly StyledProperty<double> AngleYProperty = AvaloniaProperty.Register<SkewTransform, double>("AngleY", 0.0);

	/// <summary>
	/// Gets or sets the AngleX property.
	/// </summary>
	public double AngleX
	{
		get
		{
			return GetValue(AngleXProperty);
		}
		set
		{
			SetValue(AngleXProperty, value);
		}
	}

	/// <summary>
	/// Gets or sets the AngleY property.
	/// </summary>
	public double AngleY
	{
		get
		{
			return GetValue(AngleYProperty);
		}
		set
		{
			SetValue(AngleYProperty, value);
		}
	}

	/// <summary>
	/// Gets the transform's <see cref="T:Avalonia.Matrix" />.
	/// </summary>
	public override Matrix Value => Matrix.CreateSkew(Matrix.ToRadians(AngleX), Matrix.ToRadians(AngleY));

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.SkewTransform" /> class.
	/// </summary>
	public SkewTransform()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.SkewTransform" /> class.
	/// </summary>
	/// <param name="angleX">The skew angle of X-axis, in degrees.</param>
	/// <param name="angleY">The skew angle of Y-axis, in degrees.</param>
	public SkewTransform(double angleX, double angleY)
		: this()
	{
		AngleX = angleX;
		AngleY = angleY;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == AngleXProperty || change.Property == AngleYProperty)
		{
			RaiseChanged();
		}
	}
}
