using System.Collections.Specialized;
using Avalonia;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the base class for an object that sizes and arranges child elements for a host and supports virtualization.
/// </summary>
public abstract class FAVirtualizingLayout : FALayout
{
	/// <summary>
	/// When overridden in a derived class, initializes any per-container state the layout requires when it is attached to a UIElement container.
	/// </summary>
	protected internal abstract void InitializeForContextCore(FAVirtualizingLayoutContext context);

	/// <summary>
	/// When overridden in a derived class, removes any state the layout previously stored on the UIElement container.
	/// </summary>
	protected internal abstract void UninitializeForContextCore(FAVirtualizingLayoutContext context);

	/// <summary>
	/// Provides the behavior for the "Measure" pass of the layout cycle. Classes can override this method to define their own "Measure" pass behavior.
	/// </summary>
	protected internal abstract Size MeasureOverride(FAVirtualizingLayoutContext context, Size availableSize);

	/// <summary>
	/// When implemented in a derived class, provides the behavior for the "Arrange" pass of layout. 
	/// Classes can override this method to define their own "Arrange" pass behavior.
	/// </summary>
	protected internal virtual Size ArrangeOverride(FAVirtualizingLayoutContext context, Size finalSize)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return finalSize;
	}

	protected internal virtual void OnItemsChangedCore(FAVirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
	{
		InvalidateMeasure();
	}
}
