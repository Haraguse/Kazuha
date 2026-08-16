using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Platform;
using SkiaSharp;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents an icon source that uses a bitmap as its content.
/// </summary>
public class FABitmapIconSource : FAIconSource, IDisposable
{
	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FABitmapIconSource.UriSource" /> property
	/// </summary>
	public static readonly StyledProperty<Uri> UriSourceProperty = FABitmapIcon.UriSourceProperty.AddOwner<FABitmapIconSource>((StyledPropertyMetadata<Uri>)null);

	/// <summary>
	/// Defines the <see cref="P:FluentAvalonia.UI.Controls.FABitmapIconSource.ShowAsMonochrome" /> property
	/// </summary>
	public static readonly StyledProperty<bool> ShowAsMonochromeProperty = FABitmapIcon.ShowAsMonochromeProperty.AddOwner<FABitmapIconSource>((StyledPropertyMetadata<bool>)null);

	protected internal SKBitmap _bitmap;

	private Size _originalSize;

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

	public Size Size
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _originalSize;
		}
	}

	public event EventHandler<object> OnBitmapChanged;

	~FABitmapIconSource()
	{
		try
		{
			Dispose();
		}
		finally
		{
			((object)this).Finalize();
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		((AvaloniaObject)this).OnPropertyChanged(change);
		if (change.Property == (AvaloniaProperty)(object)UriSourceProperty)
		{
			CreateBitmap(AvaloniaPropertyChangedExtensions.GetNewValue<Uri>(change));
		}
		else if (change.Property == (AvaloniaProperty)(object)ShowAsMonochromeProperty)
		{
			OnBitmapChanged?.Invoke(this, null);
		}
	}

	public void Dispose()
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

	private void CreateBitmap(Uri src)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		Dispose();
		if (src == null)
		{
			OnBitmapChanged?.Invoke(this, null);
			return;
		}
		if (src.IsAbsoluteUri && src.IsFile)
		{
			_bitmap = SKBitmap.Decode(src.LocalPath);
		}
		else
		{
			_bitmap = SKBitmap.Decode(AssetLoader.Open(src, (Uri)null));
		}
		_originalSize = new Size((double)_bitmap.Width, (double)_bitmap.Height);
		OnBitmapChanged?.Invoke(this, null);
	}
}
