using System;
using Avalonia;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the base class for an object that sizes and arranges child elements for a host.
/// </summary>
public abstract class FALayout : AvaloniaObject
{
	internal string LayoutId { get; set; }

	/// <summary>
	///
	/// </summary>
	public FAIndexBasedLayoutOrientation IndexBasedLayoutOrientation { get; protected internal set; }

	/// <summary>
	/// Occurs when the measurement state (layout) has been invalidated.
	/// </summary>
	public event TypedEventHandler<FALayout, EventArgs> MeasureInvalidated;

	/// <summary>
	/// Occurs when the arrange state(layout) has been invalidated.
	/// </summary>
	public event TypedEventHandler<FALayout, EventArgs> ArrangeInvalidated;

	private static FAVirtualizingLayoutContext GetVirtualizingLayoutContext(FALayoutContext context)
	{
		if (context is FAVirtualizingLayoutContext result)
		{
			return result;
		}
		if (context is FANonVirtualizingLayoutContext fANonVirtualizingLayoutContext)
		{
			return fANonVirtualizingLayoutContext.GetVirtualizingContextAdapter();
		}
		throw new NotImplementedException();
	}

	private static FANonVirtualizingLayoutContext GetNonVirtualizingLayoutContext(FALayoutContext context)
	{
		if (context is FANonVirtualizingLayoutContext result)
		{
			return result;
		}
		if (context is FAVirtualizingLayoutContext fAVirtualizingLayoutContext)
		{
			return fAVirtualizingLayoutContext.GetNonVirtualizingContextAdapter();
		}
		throw new NotImplementedException();
	}

	/// <summary>
	/// Initializes any per-container state the layout requires when it is attached to a UIElement container.
	/// </summary>
	public void InitializeForContext(FALayoutContext context)
	{
		if (this is FAVirtualizingLayout fAVirtualizingLayout)
		{
			FAVirtualizingLayoutContext virtualizingLayoutContext = GetVirtualizingLayoutContext(context);
			fAVirtualizingLayout.InitializeForContextCore(virtualizingLayoutContext);
			return;
		}
		if (this is FANonVirtualizingLayout fANonVirtualizingLayout)
		{
			FANonVirtualizingLayoutContext nonVirtualizingLayoutContext = GetNonVirtualizingLayoutContext(context);
			fANonVirtualizingLayout.InitializeForContextCore(nonVirtualizingLayoutContext);
			return;
		}
		throw new NotImplementedException();
	}

	/// <summary>
	/// Removes any state the layout previously stored on the UIElement container.
	/// </summary>
	public void UninitializeForContext(FALayoutContext context)
	{
		if (this is FAVirtualizingLayout fAVirtualizingLayout)
		{
			FAVirtualizingLayoutContext virtualizingLayoutContext = GetVirtualizingLayoutContext(context);
			fAVirtualizingLayout.UninitializeForContextCore(virtualizingLayoutContext);
			return;
		}
		if (this is FANonVirtualizingLayout fANonVirtualizingLayout)
		{
			FANonVirtualizingLayoutContext nonVirtualizingLayoutContext = GetNonVirtualizingLayoutContext(context);
			fANonVirtualizingLayout.UninitializeForContextCore(nonVirtualizingLayoutContext);
			return;
		}
		throw new NotImplementedException();
	}

	/// <summary>
	/// Suggests a DesiredSize for a container element. A container element that supports attached layouts 
	/// should call this method from their own MeasureOverride implementations to form a recursive layout update. 
	/// The attached layout is expected to call the Measure for each of the container’s UIElement children.
	/// </summary>
	public Size Measure(FALayoutContext context, Size availableSize)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (this is FAVirtualizingLayout fAVirtualizingLayout)
		{
			FAVirtualizingLayoutContext virtualizingLayoutContext = GetVirtualizingLayoutContext(context);
			return fAVirtualizingLayout.MeasureOverride(virtualizingLayoutContext, availableSize);
		}
		if (this is FANonVirtualizingLayout fANonVirtualizingLayout)
		{
			FANonVirtualizingLayoutContext nonVirtualizingLayoutContext = GetNonVirtualizingLayoutContext(context);
			return fANonVirtualizingLayout.MeasureOverride(nonVirtualizingLayoutContext, availableSize);
		}
		throw new NotImplementedException();
	}

	/// <summary>
	/// Positions child elements and determines a size for a container UIElement. Container elements that 
	/// support attached layouts should call this method from their layout override implementations to 
	/// form a recursive layout update.
	/// </summary>
	public Size Arrange(FALayoutContext context, Size finalSize)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (this is FAVirtualizingLayout fAVirtualizingLayout)
		{
			FAVirtualizingLayoutContext virtualizingLayoutContext = GetVirtualizingLayoutContext(context);
			return fAVirtualizingLayout.ArrangeOverride(virtualizingLayoutContext, finalSize);
		}
		if (this is FANonVirtualizingLayout fANonVirtualizingLayout)
		{
			FANonVirtualizingLayoutContext nonVirtualizingLayoutContext = GetNonVirtualizingLayoutContext(context);
			return fANonVirtualizingLayout.ArrangeOverride(nonVirtualizingLayoutContext, finalSize);
		}
		throw new NotImplementedException();
	}

	/// <summary>
	/// Invalidates the measurement state (layout) for all UIElement containers that reference this layout.
	/// </summary>
	protected void InvalidateMeasure()
	{
		MeasureInvalidated?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Invalidates the arrange state (layout) for all UIElement containers that reference this layout. 
	/// After the invalidation, the UIElement will have its layout updated, which occurs asynchronously.
	/// </summary>
	protected void InvalidateArrange()
	{
		ArrangeInvalidated?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	///
	/// </summary>
	protected internal virtual FAItemCollectionTransitionProvider CreateDefaultItemTransitionProvider()
	{
		return null;
	}
}
