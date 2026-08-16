using System;
using Avalonia.Platform;

namespace Avalonia.Media;

/// <summary>
/// Represents a 2-D geometric shape defined by the combination of two Geometry objects.
/// </summary>
public class CombinedGeometry : Geometry
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.CombinedGeometry.Geometry1" /> property.
	/// </summary>
	public static readonly StyledProperty<Geometry?> Geometry1Property;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.CombinedGeometry.Geometry2" /> property.
	/// </summary>
	public static readonly StyledProperty<Geometry?> Geometry2Property;

	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.CombinedGeometry.GeometryCombineMode" /> property.
	/// </summary>
	public static readonly StyledProperty<GeometryCombineMode> GeometryCombineModeProperty;

	/// <summary>
	/// Gets or sets the first <see cref="T:Avalonia.Media.Geometry" /> object of this
	/// <see cref="T:Avalonia.Media.CombinedGeometry" /> object.
	/// </summary>
	public Geometry? Geometry1
	{
		get
		{
			return GetValue(Geometry1Property);
		}
		set
		{
			SetValue(Geometry1Property, value);
		}
	}

	/// <summary>
	/// Gets or sets the second <see cref="T:Avalonia.Media.Geometry" /> object of this
	/// <see cref="T:Avalonia.Media.CombinedGeometry" /> object.
	/// </summary>
	public Geometry? Geometry2
	{
		get
		{
			return GetValue(Geometry2Property);
		}
		set
		{
			SetValue(Geometry2Property, value);
		}
	}

	/// <summary>
	/// Gets or sets the method by which the two geometries (specified by the
	/// <see cref="P:Avalonia.Media.CombinedGeometry.Geometry1" /> and <see cref="P:Avalonia.Media.CombinedGeometry.Geometry2" /> properties) are combined. The
	/// default value is <see cref="F:Avalonia.Media.GeometryCombineMode.Union" />.
	/// </summary>
	public GeometryCombineMode GeometryCombineMode
	{
		get
		{
			return GetValue(GeometryCombineModeProperty);
		}
		set
		{
			SetValue(GeometryCombineModeProperty, value);
		}
	}

	static CombinedGeometry()
	{
		Geometry1Property = AvaloniaProperty.Register<CombinedGeometry, Geometry>("Geometry1");
		Geometry2Property = AvaloniaProperty.Register<CombinedGeometry, Geometry>("Geometry2");
		GeometryCombineModeProperty = AvaloniaProperty.Register<CombinedGeometry, GeometryCombineMode>("GeometryCombineMode", GeometryCombineMode.Union);
		Geometry.AffectsGeometry(Geometry1Property, Geometry2Property, GeometryCombineModeProperty);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.CombinedGeometry" /> class.
	/// </summary>
	public CombinedGeometry()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.CombinedGeometry" /> class with the
	/// specified <see cref="T:Avalonia.Media.Geometry" /> objects.
	/// </summary>
	/// <param name="geometry1">The first geometry to combine.</param>
	/// <param name="geometry2">The second geometry to combine.</param>
	public CombinedGeometry(Geometry geometry1, Geometry geometry2)
	{
		Geometry1 = geometry1;
		Geometry2 = geometry2;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.CombinedGeometry" /> class with the
	/// specified <see cref="T:Avalonia.Media.Geometry" /> objects and <see cref="P:Avalonia.Media.CombinedGeometry.GeometryCombineMode" />.
	/// </summary>
	/// <param name="combineMode">The method by which geometry1 and geometry2 are combined.</param>
	/// <param name="geometry1">The first geometry to combine.</param>
	/// <param name="geometry2">The second geometry to combine.</param>
	public CombinedGeometry(GeometryCombineMode combineMode, Geometry? geometry1, Geometry? geometry2)
	{
		Geometry1 = geometry1;
		Geometry2 = geometry2;
		GeometryCombineMode = combineMode;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Media.CombinedGeometry" /> class with the
	/// specified <see cref="T:Avalonia.Media.Geometry" /> objects, <see cref="P:Avalonia.Media.CombinedGeometry.GeometryCombineMode" /> and
	/// <see cref="T:Avalonia.Media.Transform" />.
	/// </summary>
	/// <param name="combineMode">The method by which geometry1 and geometry2 are combined.</param>
	/// <param name="geometry1">The first geometry to combine.</param>
	/// <param name="geometry2">The second geometry to combine.</param>
	/// <param name="transform">The transform applied to the geometry.</param>
	public CombinedGeometry(GeometryCombineMode combineMode, Geometry? geometry1, Geometry? geometry2, Transform? transform)
	{
		Geometry1 = geometry1;
		Geometry2 = geometry2;
		GeometryCombineMode = combineMode;
		base.Transform = transform;
	}

	public override Geometry Clone()
	{
		return new CombinedGeometry(GeometryCombineMode, Geometry1, Geometry2, base.Transform);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == Geometry1Property || change.Property == Geometry2Property)
		{
			var (geometry, geometry2) = change.GetOldAndNewValue<Geometry>();
			if (geometry != null)
			{
				geometry.Changed -= ChildGeometryChanged;
			}
			if (geometry2 != null)
			{
				geometry2.Changed += ChildGeometryChanged;
			}
		}
	}

	private void ChildGeometryChanged(object? sender, EventArgs e)
	{
		InvalidateGeometry();
	}

	private protected sealed override IGeometryImpl? CreateDefiningGeometry()
	{
		Geometry geometry = Geometry1;
		Geometry geometry2 = Geometry2;
		if (geometry?.PlatformImpl != null && geometry2?.PlatformImpl != null)
		{
			return AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>().CreateCombinedGeometry(GeometryCombineMode, geometry.PlatformImpl, geometry2.PlatformImpl);
		}
		if (GeometryCombineMode == GeometryCombineMode.Intersect)
		{
			return null;
		}
		object obj = geometry?.PlatformImpl;
		if (obj == null)
		{
			if (geometry2 == null)
			{
				return null;
			}
			obj = geometry2.PlatformImpl;
		}
		return (IGeometryImpl?)obj;
	}
}
