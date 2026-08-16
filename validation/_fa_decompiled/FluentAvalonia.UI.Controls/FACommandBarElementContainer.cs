using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a container that allows an element that doesn't implement ICommandBarElement 
/// to be displayed in a command bar.
/// </summary>
[PseudoClasses(new string[] { ":overflow" })]
public class FACommandBarElementContainer : ContentControl, IFACommandBarElement
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarElementContainer.IsInOverflow" /> property
	/// </summary>
	public static readonly DirectProperty<FACommandBarElementContainer, bool> IsInOverflowProperty = AvaloniaProperty.RegisterDirect<FACommandBarElementContainer, bool>("IsInOverflow", (Func<FACommandBarElementContainer, bool>)((FACommandBarElementContainer x) => x.IsInOverflow), (Action<FACommandBarElementContainer, bool>)null, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarElementContainer.DynamicOverflowOrder" /> property
	/// </summary>
	public static readonly DirectProperty<FACommandBarElementContainer, int> DynamicOverflowOrderProperty = AvaloniaProperty.RegisterDirect<FACommandBarElementContainer, int>("DynamicOverflowOrder", (Func<FACommandBarElementContainer, int>)((FACommandBarElementContainer x) => x.DynamicOverflowOrder), (Action<FACommandBarElementContainer, int>)delegate(FACommandBarElementContainer x, int v)
	{
		x.DynamicOverflowOrder = v;
	}, 0, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarElementContainer.IsCompact" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<FACommandBarElementContainer, bool>("IsCompact", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	private bool _isInOverflow;

	private int _dynamicOverflowOrder;

	protected override Type StyleKeyOverride => typeof(FACommandBarElementContainer);

	public bool IsCompact
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(IsCompactProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(IsCompactProperty, value, (BindingPriority)0);
		}
	}

	public bool IsInOverflow
	{
		get
		{
			return _isInOverflow;
		}
		internal set
		{
			if (((AvaloniaObject)this).SetAndRaise<bool>((DirectPropertyBase<bool>)(object)IsInOverflowProperty, ref _isInOverflow, value))
			{
				PseudoClassesExtensions.Set(((StyledElement)this).PseudoClasses, ":overflow", value);
			}
		}
	}

	public int DynamicOverflowOrder
	{
		get
		{
			return _dynamicOverflowOrder;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<int>((DirectPropertyBase<int>)(object)DynamicOverflowOrderProperty, ref _dynamicOverflowOrder, value);
		}
	}

	protected override bool RegisterContentPresenter(ContentPresenter presenter)
	{
		if (((StyledElement)presenter).Name == "ContentPresenter")
		{
			return true;
		}
		return ((ContentControl)this).RegisterContentPresenter(presenter);
	}
}
