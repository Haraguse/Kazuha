using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace FluentAvalonia.UI.Controls.Primitives;

/// <summary>
/// Represents a panel that arranges the buttons in a <see cref="T:FluentAvalonia.UI.Controls.FATaskDialog" />
/// </summary>
public class FATaskDialogButtonsPanel : Panel
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.Primitives.FATaskDialogButtonsPanel.Spacing" /> property
	/// </summary>
	public static readonly DirectProperty<FATaskDialogButtonsPanel, double> SpacingProperty = AvaloniaProperty.RegisterDirect<FATaskDialogButtonsPanel, double>("Spacing", (Func<FATaskDialogButtonsPanel, double>)((FATaskDialogButtonsPanel x) => x.Spacing), (Action<FATaskDialogButtonsPanel, double>)delegate(FATaskDialogButtonsPanel x, double v)
	{
		x.Spacing = v;
	}, 0.0, (BindingMode)1, false);

	private double _spacing;

	/// <summary>
	/// Gets or sets the spacing between the buttons
	/// </summary>
	public double Spacing
	{
		get
		{
			return _spacing;
		}
		set
		{
			((AvaloniaObject)this).SetAndRaise<double>((DirectPropertyBase<double>)(object)SpacingProperty, ref _spacing, value);
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		double num = 0.0;
		double num2 = 0.0;
		int count = ((AvaloniaList<Control>)(object)((Panel)this).Children).Count;
		for (int i = 0; i < count; i++)
		{
			((Layoutable)((AvaloniaList<Control>)(object)((Panel)this).Children)[i]).Measure(Size.Infinity);
			Size desiredSize = ((Layoutable)((AvaloniaList<Control>)(object)((Panel)this).Children)[i]).DesiredSize;
			num += ((Size)(ref desiredSize)).Width;
			num2 = Math.Max(num2, ((Size)(ref desiredSize)).Height);
		}
		num += _spacing * (double)(count - 1);
		return new Size(num, num2);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		//IL_00lumn: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		int count = ((AvaloniaList<Control>)(object)((Panel)this).Children).Count;
		switch (count)
		{
		case 1:
		{
			double num4 = ((Size)(ref finalSize)).Width / 2.0;
			Rect val2 = default(Rect);
			((Rect)(ref val2))._002Ector(num4, 0.0, num4, ((Size)(ref finalSize)).Height);
			((Layoutable)((AvaloniaList<Control>)(object)((Panel)this).Children)[0]).Arrange(val2);
			break;
		}
		default:
		{
			double num = _spacing * (double)(count - 1);
			double num2 = (((Size)(ref finalSize)).Width - num) / (double)count;
			double num3 = 0.0;
			Rect val = default(Rect);
			for (int i = 0; i < count; i++)
			{
				((Rect)(ref val))._002Ector(num3, 0.0, num2, ((Size)(ref finalSize)).Height);
				((Layoutable)((AvaloniaList<Control>)(object)((Panel)this).Children)[i]).Arrange(val);
				num3 += ((Rect)(ref val)).Width + _spacing;
			}
			break;
		}
		case 0:
			break;
		}
		return finalSize;
	}
}
