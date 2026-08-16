using System;
using System.Numerics;

namespace Avalonia.Media;

/// <summary>
///  Non-Affine 3D transformation for rotating a visual around a definable axis
/// </summary>
public sealed class Rotate3DTransform : Transform
{
	private readonly bool _isInitializing;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Rotate3DTransform.AngleX" /> property.
	/// </summary>
	public static readonly StyledProperty<double> AngleXProperty = AvaloniaProperty.Register<Rotate3DTransform, double>("AngleX", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Rotate3DTransform.AngleY" /> property.
	/// </summary>
	public static readonly StyledProperty<double> AngleYProperty = AvaloniaProperty.Register<Rotate3DTransform, double>("AngleY", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Rotate3DTransform.AngleZ" /> property.
	/// </summary>
	public static readonly StyledProperty<double> AngleZProperty = AvaloniaProperty.Register<Rotate3DTransform, double>("AngleZ", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Rotate3DTransform.CenterX" /> property.
	/// </summary>
	public static readonly StyledProperty<double> CenterXProperty = AvaloniaProperty.Register<Rotate3DTransform, double>("CenterX", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Rotate3DTransform.CenterY" /> property.
	/// </summary>
	public static readonly StyledProperty<double> CenterYProperty = AvaloniaProperty.Register<Rotate3DTransform, double>("CenterY", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Rotate3DTransform.CenterZ" /> property.
	/// </summary>
	public static readonly StyledProperty<double> CenterZProperty = AvaloniaProperty.Register<Rotate3DTransform, double>("CenterZ", 0.0);

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Rotate3DTransform.Depth" /> property.
	/// </summary>
	public static readonly StyledProperty<double> DepthProperty = AvaloniaProperty.Register<Rotate3DTransform, double>("Depth", 0.0);

	/// <summary>
	/// Sets the rotation around the X-Axis
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
	/// Sets the rotation around the Y-Axis
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
	///  Sets the rotation around the Z-Axis
	/// </summary>
	public double AngleZ
	{
		get
		{
			return GetValue(AngleZProperty);
		}
		set
		{
			SetValue(AngleZProperty, value);
		}
	}

	/// <summary>
	///  Moves the origin the X-Axis rotates around
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
	///  Moves the origin the Y-Axis rotates around
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
	///  Moves the origin the Z-Axis rotates around
	/// </summary>
	public double CenterZ
	{
		get
		{
			return GetValue(CenterZProperty);
		}
		set
		{
			SetValue(CenterZProperty, value);
		}
	}

	/// <summary>
	///  Affects the depth of the rotation effect
	/// </summary>
	public double Depth
	{
		get
		{
			return GetValue(DepthProperty);
		}
		set
		{
			SetValue(DepthProperty, value);
		}
	}

	/// <summary>
	/// Gets the transform's <see cref="T:Avalonia.Matrix" />. 
	/// </summary>
	public override Matrix Value
	{
		get
		{
			Matrix4x4 identity = Matrix4x4.Identity;
			double centerX = CenterX;
			double centerY = CenterY;
			double centerZ = CenterZ;
			double angleX = AngleX;
			double angleY = AngleY;
			double angleZ = AngleZ;
			double depth = Depth;
			double num = angleZ;
			double num2 = angleY;
			double num3 = angleX;
			double num4 = centerZ;
			double num5 = centerY;
			double num6 = centerX;
			double value = num6 + num5 + num4;
			if (Math.Abs(value) > double.Epsilon)
			{
				identity *= Matrix4x4.CreateTranslation(0f - (float)num6, 0f - (float)num5, 0f - (float)num4);
			}
			if (num3 != 0.0)
			{
				identity *= Matrix4x4.CreateRotationX((float)Matrix.ToRadians(num3));
			}
			if (num2 != 0.0)
			{
				identity *= Matrix4x4.CreateRotationY((float)Matrix.ToRadians(num2));
			}
			if (num != 0.0)
			{
				identity *= Matrix4x4.CreateRotationZ((float)Matrix.ToRadians(num));
			}
			if (Math.Abs(value) > double.Epsilon)
			{
				identity *= Matrix4x4.CreateTranslation((float)num6, (float)num5, (float)num4);
			}
			if (depth != 0.0)
			{
				Matrix4x4 identity2 = Matrix4x4.Identity;
				identity2.M34 = -1f / (float)depth;
				identity *= identity2;
			}
			return new Matrix(identity.M11, identity.M12, identity.M14, identity.M21, identity.M22, identity.M24, identity.M41, identity.M42, identity.M44);
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Rotate3DTransform" /> class.
	/// </summary>
	public Rotate3DTransform()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.Rotate3DTransform" /> class.
	/// </summary>
	/// <param name="angleX">The rotation around the X-Axis</param>
	/// <param name="angleY">The rotation around the Y-Axis</param>
	/// <param name="angleZ">The rotation around the Z-Axis</param>
	/// <param name="centerX">The origin of the X-Axis</param>
	/// <param name="centerY">The origin of the Y-Axis</param>
	/// <param name="centerZ">The origin of the Z-Axis</param>
	/// <param name="depth">The depth of the 3D effect</param>
	public Rotate3DTransform(double angleX, double angleY, double angleZ, double centerX, double centerY, double centerZ, double depth)
		: this()
	{
		_isInitializing = true;
		AngleX = angleX;
		AngleY = angleY;
		AngleZ = angleZ;
		CenterX = centerX;
		CenterY = centerY;
		CenterZ = centerZ;
		Depth = depth;
		_isInitializing = false;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (!_isInitializing)
		{
			RaiseChanged();
		}
	}
}
