using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Media;

/// <summary>
/// Represents a composite geometry, composed of other <see cref="T:Avalonia.Media.Geometry" /> objects.
/// </summary>
public class GeometryGroup : Geometry
{
	public static readonly DirectProperty<GeometryGroup, GeometryCollection> ChildrenProperty = AvaloniaProperty.RegisterDirect("Children", (GeometryGroup o) => o.Children, delegate(GeometryGroup o, GeometryCollection v)
	{
		o.Children = v;
	});

	public static readonly StyledProperty<FillRule> FillRuleProperty = AvaloniaProperty.Register<GeometryGroup, FillRule>("FillRule", FillRule.EvenOdd);

	private GeometryCollection _children;

	/// <summary>
	/// Gets or sets the collection that contains the child geometries.
	/// </summary>
	[Content]
	public GeometryCollection Children
	{
		get
		{
			return _children;
		}
		set
		{
			OnChildrenChanged(_children, value);
			SetAndRaise(ChildrenProperty, ref _children, value);
		}
	}

	/// <summary>
	/// Gets or sets how the intersecting areas of the objects contained in this
	/// <see cref="T:Avalonia.Media.GeometryGroup" /> are combined. The default is <see cref="F:Avalonia.Media.FillRule.EvenOdd" />.
	/// </summary>
	public FillRule FillRule
	{
		get
		{
			return GetValue(FillRuleProperty);
		}
		set
		{
			SetValue(FillRuleProperty, value);
		}
	}

	public GeometryGroup()
	{
		_children = new GeometryCollection
		{
			Parent = this
		};
	}

	public override Geometry Clone()
	{
		GeometryGroup geometryGroup = new GeometryGroup
		{
			FillRule = FillRule,
			Transform = base.Transform
		};
		if (_children.Count > 0)
		{
			geometryGroup.Children = new GeometryCollection(_children);
		}
		return geometryGroup;
	}

	protected void OnChildrenChanged(GeometryCollection oldChildren, GeometryCollection newChildren)
	{
		oldChildren.Parent = null;
		newChildren.Parent = this;
	}

	private protected sealed override IGeometryImpl? CreateDefiningGeometry()
	{
		if (_children.Count > 0)
		{
			IPlatformRenderInterface requiredService = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
			IGeometryImpl[] array = new IGeometryImpl[_children.Count];
			for (int i = 0; i < _children.Count; i++)
			{
				array[i] = _children[i].PlatformImpl;
			}
			return requiredService.CreateGeometryGroup(FillRule, array);
		}
		return null;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		string name = change.Property.Name;
		if (name == "FillRule" || name == "Children")
		{
			InvalidateGeometry();
		}
	}

	internal void Invalidate()
	{
		InvalidateGeometry();
	}
}
