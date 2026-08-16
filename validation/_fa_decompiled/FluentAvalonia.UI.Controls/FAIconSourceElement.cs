using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.LogicalTree;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an icon that uses an IconSource as its content.
/// </summary>
public class FAIconSourceElement : FAIconElement
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAIconSourceElement.IconSource" /> property
	/// </summary>
	public static readonly StyledProperty<FAIconSource> IconSourceProperty = AvaloniaProperty.Register<FAIconSourceElement, FAIconSource>("IconSource", (FAIconSource)null, false, (BindingMode)1, (Func<FAIconSource, bool>)null, (Func<AvaloniaObject, FAIconSource, FAIconSource>)null, false);

	private Control _child;

	/// <summary>
	/// Gets or sets the IconSource used as the icon content.
	/// </summary>
	public FAIconSource IconSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<FAIconSource>(IconSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<FAIconSource>(IconSourceProperty, value, (BindingPriority)0);
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)IconSourceProperty)
		{
			OnIconSourceChanged(change);
			((Layoutable)this).InvalidateMeasure();
		}
	}

	private void OnIconSourceChanged(AvaloniaPropertyChangedEventArgs args)
	{
		FAIconSource fAIconSource = (FAIconSource)args.NewValue;
		if (_child != null)
		{
			((ISetLogicalParent)_child).SetParent((ILogical)null);
			((ICollection<ILogical>)((StyledElement)this).LogicalChildren).Clear();
			((ICollection<Visual>)((Visual)this).VisualChildren).Remove((Visual)(object)_child);
		}
		if (fAIconSource != null)
		{
			_child = (Control)(object)FAIconHelpers.CreateFromUnknown(fAIconSource);
			if (_child != null)
			{
				((ISetLogicalParent)_child).SetParent((ILogical)(object)this);
				((ICollection<Visual>)((Visual)this).VisualChildren).Add((Visual)(object)_child);
				((ICollection<ILogical>)((StyledElement)this).LogicalChildren).Add((ILogical)(object)_child);
			}
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return LayoutHelper.MeasureChild((Layoutable)(object)_child, availableSize, default(Thickness));
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return LayoutHelper.ArrangeChild((Layoutable)(object)_child, finalSize, default(Thickness));
	}
}
