namespace Avalonia.Media;

/// <summary>
/// Translates (moves) an <see cref="T:Avalonia.Visual" />.
/// </summary>
public sealed class TranslateTransform : Transform
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TranslateTransform.X" /> property.
	/// </summary>
	public static readonly StyledProperty<double> XProperty = AvaloniaProperty.Register<TranslateTransform, double>("X", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.TranslateTransform.Y" /> property.
	/// </summary>
	public static readonly StyledProperty<double> YProperty = AvaloniaProperty.Register<TranslateTransform, double>("Y", 0.0);

	/// <summary>
	/// Gets the horizontal offset of the translate.
	/// </summary>
	public double X
	{
		get
		{
			return GetValue(XProperty);
		}
		set
		{
			SetValue(XProperty, value);
		}
	}

	/// <summary>
	/// Gets the vertical offset of the translate.
	/// </summary>
	public double Y
	{
		get
		{
			return GetValue(YProperty);
		}
		set
		{
			SetValue(YProperty, value);
		}
	}

	/// <summary>
	/// Gets the transform's <see cref="T:Avalonia.Matrix" />.
	/// </summary>
	public override Matrix Value => Matrix.CreateTranslation(X, Y);

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.TranslateTransform" /> class.
	/// </summary>
	public TranslateTransform()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.TranslateTransform" /> class.
	/// </summary>
	/// <param name="x">Gets the horizontal offset of the translate.</param>
	/// <param name="y">Gets the vertical offset of the translate.</param>
	public TranslateTransform(double x, double y)
		: this()
	{
		X = x;
		Y = y;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == XProperty || change.Property == YProperty)
		{
			RaiseChanged();
		}
	}
}
