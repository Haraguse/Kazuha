using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents a separator in between items in a <see cref="T:FluentAvalonia.UI.Controls.FACommandBar" />
/// </summary>
[PseudoClasses(new string[] { ":overflow" })]
public class FACommandBarSeparator : TemplatedControl, IFACommandBarElement
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarSeparator.IsInOverflow" /> property
	/// </summary>
	public static readonly DirectProperty<FACommandBarSeparator, bool> IsInOverflowProperty = AvaloniaProperty.RegisterDirect<FACommandBarSeparator, bool>("IsInOverflow", (Func<FACommandBarSeparator, bool>)((FACommandBarSeparator x) => x.IsInOverflow), (Action<FACommandBarSeparator, bool>)null, false, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarSeparator.DynamicOverflowOrder" /> property
	/// </summary>
	public static readonly DirectProperty<FACommandBarSeparator, int> DynamicOverflowOrderProperty = AvaloniaProperty.RegisterDirect<FACommandBarSeparator, int>("DynamicOverflowOrder", (Func<FACommandBarSeparator, int>)((FACommandBarSeparator x) => x.DynamicOverflowOrder), (Action<FACommandBarSeparator, int>)delegate(FACommandBarSeparator x, int v)
	{
		x.DynamicOverflowOrder = v;
	}, 0, (BindingMode)1, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FACommandBarSeparator.IsCompact" /> property
	/// </summary>
	public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<FACommandBarSeparator, bool>("IsCompact", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	private bool _isInOverflow;

	private int _dynamicOverflowOrder;

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
}
