using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media;

/// <summary>
/// Defines a geometric shape.
/// </summary>
[TypeConverter(typeof(GeometryTypeConverter))]
public abstract class Geometry : AvaloniaObject, ICompositionRenderResource<ServerCompositionSimpleGeometry>, ICompositionRenderResource, ICompositorSerializable
{
	/// <summary>
	/// Defines the <see cref="P:Avalonia.Media.Geometry.Transform" /> property.
	/// </summary>
	public static readonly StyledProperty<Transform?> TransformProperty;

	private bool _isDirty = true;

	private readonly bool _canInvaldate = true;

	private IGeometryImpl? _platformImpl;

	private CompositorResourceHolder<ServerCompositionSimpleGeometry> _resource;

	/// <summary>
	/// Gets the geometry's bounding rectangle.
	/// </summary>
	public Rect Bounds => PlatformImpl?.Bounds ?? default(Rect);

	/// <summary>
	/// Gets the platform-specific implementation of the geometry.
	/// </summary>
	internal IGeometryImpl? PlatformImpl
	{
		get
		{
			if (_isDirty)
			{
				IGeometryImpl geometryImpl = CreateDefiningGeometry();
				Transform transform = Transform;
				if (geometryImpl != null && transform != null && transform.Value != Matrix.Identity)
				{
					geometryImpl = geometryImpl.WithTransform(transform.Value);
				}
				_platformImpl = geometryImpl;
				_isDirty = false;
			}
			return _platformImpl;
		}
	}

	/// <summary>
	/// Gets or sets a transform to apply to the geometry.
	/// </summary>
	public Transform? Transform
	{
		get
		{
			return GetValue(TransformProperty);
		}
		set
		{
			SetValue(TransformProperty, value);
		}
	}

	/// <summary>
	/// Gets the geometry's total length as if all its contours are placed
	/// in a straight line.
	/// </summary>
	public double ContourLength => PlatformImpl?.ContourLength ?? 0.0;

	/// <summary>
	/// Raised when the geometry changes.
	/// </summary>
	public event EventHandler? Changed;

	static Geometry()
	{
		TransformProperty = AvaloniaProperty.Register<Geometry, Transform>("Transform");
		TransformProperty.Changed.AddClassHandler(delegate(Geometry x, AvaloniaPropertyChangedEventArgs e)
		{
			x.TransformChanged(e);
		});
	}

	internal Geometry()
	{
	}

	private protected Geometry(IGeometryImpl? platformImpl)
	{
		_platformImpl = platformImpl;
		_isDirty = (_canInvaldate = false);
	}

	/// <summary>
	/// Creates a <see cref="T:Avalonia.Media.Geometry" /> from a string.
	/// </summary>
	/// <param name="s">The string.</param>
	/// <returns>A <see cref="T:Avalonia.Media.StreamGeometry" />.</returns>
	public static Geometry Parse(string s)
	{
		return StreamGeometry.Parse(s);
	}

	/// <summary>
	/// Clones the geometry.
	/// </summary>
	/// <returns>A cloned geometry.</returns>
	public abstract Geometry Clone();

	/// <summary>
	/// Gets the geometry's bounding rectangle with the specified pen.
	/// </summary>
	/// <param name="pen">The stroke thickness.</param>
	/// <returns>The bounding rectangle.</returns>
	public Rect GetRenderBounds(IPen pen)
	{
		return PlatformImpl?.GetRenderBounds(pen) ?? default(Rect);
	}

	/// <summary>
	/// Indicates whether the geometry's fill contains the specified point.
	/// </summary>
	/// <param name="point">The point.</param>
	/// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
	public bool FillContains(Point point)
	{
		return PlatformImpl?.FillContains(point) ?? false;
	}

	/// <summary>
	/// Indicates whether the geometry's stroke contains the specified point.
	/// </summary>
	/// <param name="pen">The pen to use.</param>
	/// <param name="point">The point.</param>
	/// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
	public bool StrokeContains(IPen pen, Point point)
	{
		return PlatformImpl?.StrokeContains(pen, point) ?? false;
	}

	/// <summary>
	/// Gets a <see cref="T:Avalonia.Media.Geometry" /> that is the shape defined by the stroke on the Geometry
	/// produced by the specified Pen.
	/// </summary>
	/// <param name="pen">The pen to use.</param>
	/// <returns>The outlined geometry.</returns>
	public Geometry GetWidenedGeometry(IPen pen)
	{
		return new ImmutableGeometry(PlatformImpl?.GetWidenedGeometry(pen));
	}

	/// <summary>
	/// Marks a property as affecting the geometry's <see cref="P:Avalonia.Media.Geometry.PlatformImpl" />.
	/// </summary>
	/// <param name="properties">The properties.</param>
	/// <remarks>
	/// After a call to this method in a control's static constructor, any change to the
	/// property will cause <see cref="M:Avalonia.Media.Geometry.InvalidateGeometry" /> to be called on the element.
	/// </remarks>
	protected static void AffectsGeometry(params AvaloniaProperty[] properties)
	{
		AnonymousObserver<AvaloniaPropertyChangedEventArgs> observer = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(AffectsGeometryInvalidate);
		for (int i = 0; i < properties.Length; i++)
		{
			properties[i].Changed.Subscribe(observer);
		}
	}

	/// <summary>
	/// Creates the platform implementation of the geometry, without the transform applied.
	/// </summary>
	/// <returns></returns>
	private protected abstract IGeometryImpl? CreateDefiningGeometry();

	/// <summary>
	/// Invalidates the platform implementation of the geometry.
	/// </summary>
	protected void InvalidateGeometry()
	{
		if (_canInvaldate)
		{
			_isDirty = true;
			_platformImpl = null;
			RegisterForSerialization();
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	private void TransformChanged(AvaloniaPropertyChangedEventArgs e)
	{
		Transform transform = (Transform)e.OldValue;
		Transform transform2 = (Transform)e.NewValue;
		if (transform != null)
		{
			transform.Changed -= TransformChanged;
		}
		if (transform2 != null)
		{
			transform2.Changed += TransformChanged;
		}
		TransformChanged(transform2, EventArgs.Empty);
	}

	private void TransformChanged(object? sender, EventArgs e)
	{
		Matrix? matrix = ((Transform)sender)?.Value;
		if (_platformImpl is ITransformedGeometryImpl transformedGeometryImpl)
		{
			if (!matrix.HasValue || matrix == Matrix.Identity)
			{
				_platformImpl = transformedGeometryImpl.SourceGeometry;
			}
			else if (matrix != transformedGeometryImpl.Transform)
			{
				_platformImpl = transformedGeometryImpl.SourceGeometry.WithTransform(matrix.Value);
			}
		}
		else if (_platformImpl != null && matrix.HasValue && matrix != Matrix.Identity)
		{
			_platformImpl = _platformImpl.WithTransform(matrix.Value);
		}
		RegisterForSerialization();
		Changed?.Invoke(this, EventArgs.Empty);
	}

	private static void AffectsGeometryInvalidate(AvaloniaPropertyChangedEventArgs e)
	{
		(e.Sender as Geometry)?.InvalidateGeometry();
	}

	/// <summary>
	/// Combines the two geometries using the specified <see cref="T:Avalonia.Media.GeometryCombineMode" /> and applies the specified transform to the resulting geometry.
	/// </summary>
	/// <param name="geometry1">The first geometry to combine.</param>
	/// <param name="geometry2">The second geometry to combine.</param>
	/// <param name="combineMode">One of the enumeration values that specifies how the geometries are combined.</param>
	/// <param name="transform">A transformation to apply to the combined geometry, or <c>null</c>.</param>
	/// <returns></returns>
	public static Geometry Combine(Geometry geometry1, RectangleGeometry geometry2, GeometryCombineMode combineMode, Transform? transform = null)
	{
		return new CombinedGeometry(combineMode, geometry1, geometry2, transform);
	}

	/// <summary>
	/// Attempts to get the corresponding point at the
	/// specified distance
	/// </summary>
	/// <param name="distance">The contour distance to get from.</param>
	/// <param name="point">The point in the specified distance.</param>
	/// <returns>If there's valid point at the specified distance.</returns>
	public bool TryGetPointAtDistance(double distance, out Point point)
	{
		if (PlatformImpl == null)
		{
			point = default(Point);
			return false;
		}
		return PlatformImpl.TryGetPointAtDistance(distance, out point);
	}

	/// <summary>
	/// Attempts to get the corresponding point and
	/// tangent from the specified distance along the
	/// contour of the geometry.
	/// </summary>
	/// <param name="distance">The contour distance to get from.</param>
	/// <param name="point">The point in the specified distance.</param>
	/// <param name="tangent">The tangent in the specified distance.</param>
	/// <returns>If there's valid point and tangent at the specified distance.</returns>
	public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
	{
		if (PlatformImpl == null)
		{
			point = (tangent = default(Point));
			return false;
		}
		return PlatformImpl.TryGetPointAndTangentAtDistance(distance, out point, out tangent);
	}

	/// <summary>
	/// Attempts to get the corresponding path segment
	/// given by the two distances specified.
	/// Imagine it like snipping a part of the current
	/// geometry.
	/// </summary>
	/// <param name="startDistance">The contour distance to start snipping from.</param>
	/// <param name="stopDistance">The contour distance to stop snipping to.</param>
	/// <param name="startOnBeginFigure">If ture, the resulting snipped path will start with a BeginFigure call.</param>
	/// <param name="segmentGeometry">The resulting snipped path.</param>
	/// <returns>If the snipping operation is successful.</returns>
	public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure, [NotNullWhen(true)] out Geometry? segmentGeometry)
	{
		segmentGeometry = null;
		if (PlatformImpl == null)
		{
			return false;
		}
		if (!PlatformImpl.TryGetSegment(startDistance, stopDistance, startOnBeginFigure, out IGeometryImpl segmentGeometry2))
		{
			return false;
		}
		segmentGeometry = new PlatformGeometry(segmentGeometry2);
		return true;
	}

	private protected void RegisterForSerialization()
	{
		_resource.RegisterForInvalidationOnAllCompositors(this);
	}

	ServerCompositionSimpleGeometry ICompositionRenderResource<ServerCompositionSimpleGeometry>.GetForCompositor(Compositor c)
	{
		return _resource.GetForCompositor(c);
	}

	void ICompositionRenderResource.AddRefOnCompositor(Compositor c)
	{
		_resource.CreateOrAddRef(c, this, out ServerCompositionSimpleGeometry _, (Compositor compositor) => new ServerCompositionSimpleGeometry(compositor.Server));
	}

	void ICompositionRenderResource.ReleaseOnCompositor(Compositor c)
	{
		_resource.Release(c);
	}

	SimpleServerObject? ICompositorSerializable.TryGetServer(Compositor c)
	{
		return _resource.TryGetForCompositor(c);
	}

	void ICompositorSerializable.SerializeChanges(Compositor c, BatchStreamWriter writer)
	{
		ServerCompositionSimpleGeometry.SerializeAllChanges(writer, PlatformImpl);
	}
}
