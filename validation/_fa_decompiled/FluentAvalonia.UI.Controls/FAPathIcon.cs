using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an icon that uses a vector path as its content.
/// </summary>
/// <summary>
/// Represents an icon that uses a vector path as its content.
/// </summary>
public class FAPathIcon : FAIconElement
{
	private Matrix _transform;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAPathIcon.Data" /> property
	/// </summary>
	public static readonly StyledProperty<Geometry> DataProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAPathIcon.Stretch" /> property.
	/// </summary>
	public static readonly StyledProperty<Stretch> StretchProperty;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAPathIcon.StretchDirection" /> property.
	/// </summary>
	public static readonly StyledProperty<StretchDirection> StretchDirectionProperty;

	/// <summary>
	/// Gets or sets a Geometry that specifies the shape to be drawn. 
	/// In XAML. this can also be set using a string that describes Move and draw commands syntax.
	/// </summary>
	public Geometry Data
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Geometry>(DataProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Geometry>(DataProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a <see cref="P:FluentAvalonia.UI.Controls.FAPathIcon.Stretch" /> enumeration value that describes how the shape fills its allocated space.
	/// </summary>
	public Stretch Stretch
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<Stretch>(StretchProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<Stretch>(StretchProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value controlling in what direction contents will be stretched.
	/// </summary>
	public StretchDirection StretchDirection
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((AvaloniaObject)this).GetValue<StretchDirection>(StretchDirectionProperty);
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			((AvaloniaObject)this).SetValue<StretchDirection>(StretchDirectionProperty, value, (BindingPriority)0);
		}
	}

	static FAPathIcon()
	{
		DataProperty = Path.DataProperty.AddOwner<FAPathIcon>((StyledPropertyMetadata<Geometry>)null);
		StretchProperty = Shape.StretchProperty.AddOwner<FAPathIcon>((StyledPropertyMetadata<Stretch>)null);
		StretchDirectionProperty = Viewbox.StretchDirectionProperty.AddOwner<FAPathIcon>((StyledPropertyMetadata<StretchDirection>)null);
		StretchProperty.OverrideDefaultValue<FAPathIcon>((Stretch)2);
		StretchDirectionProperty.OverrideDefaultValue<FAPathIcon>((StretchDirection)2);
		Visual.ClipToBoundsProperty.OverrideDefaultValue<FAPathIcon>(true);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)StretchProperty || change.Property == (AvaloniaProperty)(object)StretchDirectionProperty || change.Property == (AvaloniaProperty)(object)DataProperty)
		{
			((Layoutable)this).InvalidateMeasure();
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (Data == null)
		{
			return ((Layoutable)this).MeasureOverride(availableSize);
		}
		return CalculateSizeAndTransform(availableSize, Data.Bounds, Stretch, StretchDirection).size;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (Data != null)
		{
			Matrix item = CalculateSizeAndTransform(finalSize, Data.Bounds, Stretch, StretchDirection).transform;
			if (_transform != item)
			{
				_transform = item;
			}
			return finalSize;
		}
		return default(Size);
	}

	private static (Size size, Matrix transform) CalculateSizeAndTransform(Size availableSize, Rect shapeBounds, Stretch stretch, StretchDirection stretchDirection)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Expected I4, but got Unknown
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Invalid comparison between Unknown and I4
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Expected I4, but got Unknown
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		Size size = default(Size);
		((Size)(ref size))._002Ector(((Rect)(ref shapeBounds)).Right, ((Rect)(ref shapeBounds)).Bottom);
		Matrix identity = Matrix.Identity;
		double width = ((Size)(ref availableSize)).Width;
		double height = ((Size)(ref availableSize)).Height;
		double num = 0.0;
		double num2 = 0.0;
		if ((int)stretch != 0)
		{
			size = ((Rect)(ref shapeBounds)).Size;
		}
		if (double.IsInfinity(((Size)(ref availableSize)).Width))
		{
			width = ((Size)(ref size)).Width;
		}
		if (double.IsInfinity(((Size)(ref availableSize)).Height))
		{
			height = ((Size)(ref size)).Height;
		}
		if (((Rect)(ref shapeBounds)).Width > 0.0)
		{
			num = width / ((Size)(ref size)).Width;
		}
		if (((Rect)(ref shapeBounds)).Height > 0.0)
		{
			num2 = height / ((Size)(ref size)).Height;
		}
		if (double.IsInfinity(((Size)(ref availableSize)).Width))
		{
			num = num2;
		}
		if (double.IsInfinity(((Size)(ref availableSize)).Height))
		{
			num2 = num;
		}
		switch (stretch - 1)
		{
		case 1:
			num = (num2 = Math.Min(num, num2));
			break;
		case 2:
			num = (num2 = Math.Max(num, num2));
			break;
		case 0:
			if (double.IsInfinity(((Size)(ref availableSize)).Width))
			{
				num = 1.0;
			}
			if (double.IsInfinity(((Size)(ref availableSize)).Height))
			{
				num2 = 1.0;
			}
			break;
		default:
			num = (num2 = 1.0);
			break;
		}
		if ((int)stretchDirection != 0)
		{
			if ((int)stretchDirection == 1)
			{
				if (num > 1.0)
				{
					num = 1.0;
				}
				if (num2 > 1.0)
				{
					num2 = 1.0;
				}
			}
		}
		else
		{
			if (num < 1.0)
			{
				num = 1.0;
			}
			if (num2 < 1.0)
			{
				num2 = 1.0;
			}
		}
		Point position;
		switch ((int)stretch)
		{
		case 0:
		{
			position = ((Rect)(ref shapeBounds)).Position;
			double num4 = 0.0 - ((Point)(ref position)).X - (((Rect)(ref shapeBounds)).Width - width) / 2.0;
			position = ((Rect)(ref shapeBounds)).Position;
			identity = Matrix.CreateTranslation(num4, 0.0 - ((Point)(ref position)).Y - (((Rect)(ref shapeBounds)).Height - height) / 2.0);
			break;
		}
		case 2:
		case 3:
			if (num != 0.0 && num2 != 0.0)
			{
				position = ((Rect)(ref shapeBounds)).Position;
				double num3 = 0.0 - ((Point)(ref position)).X - (((Rect)(ref shapeBounds)).Width * num - width) / num / 2.0;
				position = ((Rect)(ref shapeBounds)).Position;
				identity = Matrix.CreateTranslation(num3, 0.0 - ((Point)(ref position)).Y - (((Rect)(ref shapeBounds)).Height * num2 - height) / num2 / 2.0);
			}
			else
			{
				identity = Matrix.CreateTranslation(-Point.op_Implicit(((Rect)(ref shapeBounds)).Position));
			}
			break;
		case 1:
			identity = Matrix.CreateTranslation(-Point.op_Implicit(((Rect)(ref shapeBounds)).Position));
			break;
		default:
			throw new ArgumentOutOfRangeException("Stretch", stretch, null);
		}
		Matrix item = identity * Matrix.CreateScale(num, num2);
		return (size: new Size(((Size)(ref size)).Width * num, ((Size)(ref size)).Height * num2), transform: item);
	}

	public unsafe override void Render(DrawingContext context)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		Geometry data = Data;
		if (data == null)
		{
			return;
		}
		PushedState val = context.PushTransform(_transform);
		try
		{
			context.DrawGeometry(base.Foreground, (IPen)null, data);
		}
		finally
		{
			((IDisposable)(*(PushedState*)(&val))/*cast due to constrained. prefix*/).Dispose();
		}
	}

	/// <summary>
	/// Quick and dirty check if we have a valid PathGeometry. This probably needs to be
	/// more robust, but this is better than a bunch of InvalidDataExceptions becase we 
	/// don't have a Path.TryParse() method. This does still fail sometimes, but its better
	/// than nothing. Its really only meant to be called from the StringToIconElementConverter
	/// </summary>
	public static bool IsDataValid(string data, out Geometry g)
	{
		if (data.Length <= 1 || data.Contains(":") || data.Contains("/\\"))
		{
			g = null;
			return false;
		}
		try
		{
			string item = data[0].ToString().ToUpper();
			if (new List<string> { "M", "C", "L", "V", "H", "F" }.Contains(item) || data.Contains(" ") || data.Contains(","))
			{
				g = (Geometry)(object)StreamGeometry.Parse(data);
				return true;
			}
			StreamGeometry val = StreamGeometry.Parse(data);
			g = (Geometry)(object)val;
			return true;
		}
		catch
		{
			g = null;
			return false;
		}
	}

	public FAPathIcon()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_transform = Matrix.Identity;
		base._002Ector();
	}
}
