using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents and icon that uses a bitmap as its content
/// </summary>
public class FABitmapIcon : FAIconElement
{
	private FABitmapIconSource _bis;

	protected SKBitmap _bitmap;

	private Size _originalSize;

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FABitmapIcon.UriSource" /> property
	/// </summary>
	public static readonly StyledProperty<Uri> UriSourceProperty = AvaloniaProperty.Register<FABitmapIcon, Uri>("UriSource", (Uri)null, false, (BindingMode)1, (Func<Uri, bool>)null, (Func<AvaloniaObject, Uri, Uri>)null, false);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FABitmapIcon.ShowAsMonochrome" /> property
	/// </summary>
	public static readonly StyledProperty<bool> ShowAsMonochromeProperty = AvaloniaProperty.Register<FABitmapIcon, bool>("ShowAsMonochrome", false, false, (BindingMode)1, (Func<bool, bool>)null, (Func<AvaloniaObject, bool, bool>)null, false);

	/// <summary>
	/// Gets or sets the Uniform Resource Identifier (URI) of the bitmap to use as the icon content.
	/// </summary>
	public Uri UriSource
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<Uri>(UriSourceProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<Uri>(UriSourceProperty, value, (BindingPriority)0);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the bitmap is shown in a single color.
	/// </summary>
	public bool ShowAsMonochrome
	{
		get
		{
			return ((AvaloniaObject)this).GetValue<bool>(ShowAsMonochromeProperty);
		}
		set
		{
			((AvaloniaObject)this).SetValue<bool>(ShowAsMonochromeProperty, value, (BindingPriority)0);
		}
	}

	public FABitmapIcon()
	{
		RenderOptions.SetBitmapInterpolationMode((Visual)(object)this, (BitmapInterpolationMode)4);
	}

	~FABitmapIcon()
	{
		try
		{
			Dispose();
			UnlinkFromBitmapIconSource();
		}
		finally
		{
			((object)this).Finalize();
		}
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)UriSourceProperty)
		{
			if (_bis != null)
			{
				throw new InvalidOperationException("Cannot edit properties of BitmapIcon if BitmapIconSource is linked");
			}
			CreateBitmap(AvaloniaPropertyChangedExtensions.GetNewValue<Uri>(change));
			((Visual)this).InvalidateVisual();
		}
		else if (change.Property == (AvaloniaProperty)(object)ShowAsMonochromeProperty)
		{
			if (_bis != null)
			{
				throw new InvalidOperationException("Cannot edit properties of BitmapIcon if BitmapIconSource is linked");
			}
			((Visual)this).InvalidateVisual();
		}
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (_bis != null)
		{
			return _originalSize;
		}
		if (_bitmap == null || UriSource == null)
		{
			return ((Layoutable)this).MeasureOverride(availableSize);
		}
		return _originalSize;
	}

	/// <inheritdoc />
	public unsafe override void Render(DrawingContext context)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected O, but got Unknown
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		if (_bitmap == null && _bis == null)
		{
			return;
		}
		Rect bounds = ((Visual)this).Bounds;
		Rect val = default(Rect);
		((Rect)(ref val))._002Ector(((Rect)(ref bounds)).Size);
		if (((Rect)(ref val)).Width < 1.0 || ((Rect)(ref val)).Height < 1.0)
		{
			return;
		}
		int num = (int)((Rect)(ref val)).Width;
		int num2 = (int)((Rect)(ref val)).Height;
		WriteableBitmap val2 = new WriteableBitmap(new PixelSize(num, num2), new Vector(96.0, 96.0), (PixelFormat?)PixelFormats.Bgra8888, (AlphaFormat?)(AlphaFormat)0);
		try
		{
			ILockedFramebuffer val3 = val2.Lock();
			try
			{
				SKSurface val4 = SKSurface.Create(new SKImageInfo(num, num2), val3.Address);
				if (val4 == null)
				{
					return;
				}
				SKCanvas canvas = val4.Canvas;
				canvas.Clear(new SKColor((byte)0, (byte)0, (byte)0, (byte)0));
				SKBitmap val5 = _bitmap.Resize(new SKImageInfo(num, num2), new SKSamplingOptions(SKCubicResampler.Mitchell));
				if (ShowAsMonochrome)
				{
					IBrush foreground = base.Foreground;
					ISolidColorBrush val6 = (ISolidColorBrush)(object)((foreground is ISolidColorBrush) ? foreground : null);
					Color val7 = ((val6 != null) ? val6.Color : Colors.White);
					SKColor val8 = default(SKColor);
					((SKColor)(ref val8))._002Ector(((Color)(ref val7)).R, ((Color)(ref val7)).G, ((Color)(ref val7)).B, ((Color)(ref val7)).A);
					SKPaint val9 = new SKPaint();
					val9.ColorFilter = SKColorFilter.CreateBlendMode(val8, (SKBlendMode)9);
					canvas.DrawBitmap(val5, new SKRect(0f, 0f, (float)num, (float)num2), val9);
					((SKNativeObject)val9).Dispose();
				}
				else
				{
					canvas.DrawBitmap(val5, new SKRect(0f, 0f, (float)num, (float)num2), (SKPaint)null);
				}
				((SKNativeObject)val5).Dispose();
				PushedState val10 = context.PushClip(val);
				try
				{
					context.DrawImage((IImage)(object)val2, new Rect(((Bitmap)val2).Size), val);
				}
				finally
				{
					((IDisposable)(*(PushedState*)(&val10))/*cast due to constrained. prefix*/).Dispose();
				}
			}
			finally
			{
				((IDisposable)val3)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
	}

	private void CreateBitmap(Uri src)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		if (_bis != null)
		{
			return;
		}
		Dispose();
		if (!(src == null))
		{
			if (src.IsAbsoluteUri && src.IsFile)
			{
				_bitmap = SKBitmap.Decode(src.LocalPath);
			}
			else
			{
				_bitmap = SKBitmap.Decode(AssetLoader.Open(src, (Uri)null));
			}
			_originalSize = new Size((double)_bitmap.Width, (double)_bitmap.Height);
		}
	}

	/// <inheritdoc />
	protected void Dispose()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		SKBitmap bitmap = _bitmap;
		if (bitmap != null)
		{
			((SKNativeObject)bitmap).Dispose();
		}
		_bitmap = null;
		_originalSize = default(Size);
	}

	internal void LinkToBitmapIconSource(FABitmapIconSource bis)
	{
		if (bis == null)
		{
			throw new ArgumentNullException("BitmapIconSource", "BitmapIconSource cannot be null");
		}
		_bis = bis;
		OnLinkedBitmapIconSourceChanged(null, null);
		bis.OnBitmapChanged += OnLinkedBitmapIconSourceChanged;
	}

	internal void UnlinkFromBitmapIconSource()
	{
		if (_bis != null)
		{
			_bis.OnBitmapChanged -= OnLinkedBitmapIconSourceChanged;
		}
		_bis = null;
	}

	private void OnLinkedBitmapIconSourceChanged(object sender, object e)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		Dispose();
		_bitmap = _bis._bitmap;
		_originalSize = _bis.Size;
	}
}
