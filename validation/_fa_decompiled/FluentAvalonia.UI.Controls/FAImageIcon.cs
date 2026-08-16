using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an icon that uses an <see cref="T:Avalonia.Media.IImage" /> as its content.
/// </summary>
public class FAImageIcon : FAIconElement
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FAImageIcon.Source" /> property
	/// </summary>
	public static readonly StyledProperty<IImage> SourceProperty = AvaloniaProperty.Register<FAImageIcon, IImage>("Source", (IImage)null, false, (BindingMode)1, (Func<IImage, bool>)null, (Func<AvaloniaObject, IImage, IImage>)null, false);

	/// <summary>
	/// Gets or sets the <see cref="T:Avalonia.Media.IImage" /> content this icon displays
	/// </summary>
	[Content]
	public IImage Source
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<IImage>(SourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<IImage>(SourceProperty, value, (BindingPriority)0);
		}
	}

	public FAImageIcon()
	{
		RenderOptions.SetBitmapInterpolationMode((Visual)(object)this, (BitmapInterpolationMode)4);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)SourceProperty)
		{
			((Layoutable)this).InvalidateMeasure();
			((Visual)this).InvalidateVisual();
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		IImage source = Source;
		if (source == null)
		{
			return default(Size);
		}
		return source.Size;
	}

	public override void Render(DrawingContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00lumn: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		Rect val = ((Visual)this).Bounds;
		Size size = ((Rect)(ref val)).Size;
		IImage source = Source;
		if (source != null && ((Size)(ref size)).Width > 0.0 && ((Size)(ref size)).Height > 0.0)
		{
			Rect val2 = default(Rect);
			((Rect)(ref val2))._002Ector(size);
			double width = ((Size)(ref size)).Width;
			Size size2 = source.Size;
			double num = width / ((Size)(ref size2)).Width;
			double height = ((Size)(ref size)).Height;
			size2 = source.Size;
			Vector val3 = default(Vector);
			((Vector)(ref val3))._002Ector(num, height / ((Size)(ref size2)).Height);
			Size val4 = source.Size * val3;
			val = ((Rect)(ref val2)).CenterRect(new Rect(val4));
			Rect val5 = ((Rect)(ref val)).Intersect(val2);
			val = new Rect(source.Size);
			Rect val6 = ((Rect)(ref val)).CenterRect(new Rect(((Rect)(ref val5)).Size / val3));
			context.DrawImage(source, val6, val5);
		}
	}
}
