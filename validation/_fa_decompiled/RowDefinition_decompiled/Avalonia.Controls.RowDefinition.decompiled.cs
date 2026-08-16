namespace Avalonia.Controls;

public class RowDefinition : DefinitionBase
{
	public static readonly StyledProperty<double> MaxHeightProperty;

	public static readonly StyledProperty<double> MinHeightProperty;

	public static readonly StyledProperty<GridLength> HeightProperty;

	public double ActualHeight => base.Parent?.GetFinalRowDefinitionHeight(base.Index) ?? 0.0;

	public double MaxHeight
	{
		get
		{
			return GetValue(MaxHeightProperty);
		}
		set
		{
			SetValue(MaxHeightProperty, value);
		}
	}

	public double MinHeight
	{
		get
		{
			return GetValue(MinHeightProperty);
		}
		set
		{
			SetValue(MinHeightProperty, value);
		}
	}

	public GridLength Height
	{
		get
		{
			return GetValue(HeightProperty);
		}
		set
		{
			SetValue(HeightProperty, value);
		}
	}

	internal override GridLength UserSizeValueCache => Height;

	internal override double UserMinSizeValueCache => MinHeight;

	internal override double UserMaxSizeValueCache => MaxHeight;

	static RowDefinition()
	{
		MaxHeightProperty = AvaloniaProperty.Register<RowDefinition, double>("MaxHeight", double.PositiveInfinity);
		MinHeightProperty = AvaloniaProperty.Register<RowDefinition, double>("MinHeight", 0.0);
		HeightProperty = AvaloniaProperty.Register<RowDefinition, GridLength>("Height", new GridLength(1.0, GridUnitType.Star));
		DefinitionBase.AffectsParentMeasure(MaxHeightProperty, MinHeightProperty);
		HeightProperty.Changed.AddClassHandler<DefinitionBase>(DefinitionBase.OnUserSizePropertyChanged);
	}

	public RowDefinition()
	{
	}

	public RowDefinition(double value, GridUnitType type)
		: this(new GridLength(value, type))
	{
	}

	public RowDefinition(GridLength height)
	{
		SetCurrentValue(HeightProperty, height);
	}
}
