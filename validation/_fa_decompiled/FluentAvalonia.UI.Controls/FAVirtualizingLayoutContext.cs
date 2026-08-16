using Avalonia;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the base class for layout context types that support virtualization.
/// </summary>
public abstract class FAVirtualizingLayoutContext : FALayoutContext
{
	private FANonVirtualizingLayoutContext _contextAdapter;

	/// <summary>
	/// Gets the number of items in the data.
	/// </summary>
	public int ItemCount => ItemCountCore();

	/// <summary>
	/// Gets an area that represents the viewport and buffer that the layout should fill with realized elements.
	/// </summary>
	public Rect RealizationRect
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return RealizationRectCore();
		}
	}

	/// <summary>
	/// Gets the recommended index from which to start the generation and layout of elements.
	/// </summary>
	public int RecommendedAnchorIndex => RecommendedAnchorIndexCore();

	/// <summary>
	/// Gets or sets the origin point for the estimated content size.
	/// </summary>
	public Point LayoutOrigin
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return LayoutOriginCore();
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			LayoutOriginCore(value);
		}
	}

	/// <summary>
	/// Retrieves the data item in the source found at the specified index.
	/// </summary>
	public object GetItemAt(int index)
	{
		return GetItemAtCore(index);
	}

	/// <summary>
	/// Retrieves a UIElement that represents the data item in the source found at the specified index.By default, if an element already exists, it is returned; otherwise, a new element is created.
	/// </summary>
	public Control GetOrCreateElementAt(int index)
	{
		return GetOrCreateElementAtCore(index, FAElementRealizationOptions.None);
	}

	/// <summary>
	/// Retrieves a UIElement that represents the data item in the source found at the specified index using the specified options.
	/// </summary>
	public Control GetOrCreateElementAt(int index, FAElementRealizationOptions options)
	{
		return GetOrCreateElementAtCore(index, options);
	}

	/// <summary>
	/// Clears the specified UIElement and allows it to be either re-used or released.
	/// </summary>
	public void RecycleElement(Control element)
	{
		RecycleElementCore(element);
	}

	/// <summary>
	/// When implemented in a derived class, retrieves the data item in the source found at the specified index.
	/// </summary>
	protected abstract object GetItemAtCore(int index);

	/// <summary>
	/// When implemented in a derived class, retrieves a UIElement that represents the data item in the 
	/// source found at the specified index using the specified options.
	/// </summary>
	protected abstract Control GetOrCreateElementAtCore(int index, FAElementRealizationOptions options);

	/// <summary>
	/// When implemented in a derived class, clears the specified UIElement and allows it to be either re-used or released.
	/// </summary>
	protected abstract void RecycleElementCore(Control element);

	/// <summary>
	/// Provides the value that is assigned to the VisibleRect property.
	/// </summary>
	protected abstract Rect VisibleRectCore();

	/// <summary>
	/// When implemented in a derived class, retrieves an area that represents the viewport and buffer that the 
	/// layout should fill with realized elements.
	/// </summary>
	protected abstract Rect RealizationRectCore();

	/// <summary>
	/// Implements the behavior for getting the return value of RecommendedAnchorIndex in a derived or custom VirtualizingLayoutContext.
	/// </summary>
	protected abstract int RecommendedAnchorIndexCore();

	/// <summary>
	/// Implements the behavior of LayoutOrigin in a derived or custom VirtualizingLayoutContext.
	/// </summary>
	protected abstract Point LayoutOriginCore();

	/// <summary>
	///
	/// </summary>
	protected abstract void LayoutOriginCore(Point value);

	/// <summary>
	///
	/// </summary>
	protected internal abstract int ItemCountCore();

	internal FANonVirtualizingLayoutContext GetNonVirtualizingContextAdapter()
	{
		if (_contextAdapter == null)
		{
			_contextAdapter = new VirtualLayoutContextAdapter(this);
		}
		return _contextAdapter;
	}
}
