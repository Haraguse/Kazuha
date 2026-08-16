using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Media;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

public static class FAIconHelpers
{
	private static Dictionary<Type, Func<FAIconSource, FAIconElement>> _customConverters;

	internal static FAFontIcon CreateFontIconFromFontIconSource(FAFontIconSource fis)
	{
		FAFontIcon fAFontIcon = new FAFontIcon();
		IndexerDescriptor val = !(AvaloniaProperty)(object)TextElement.FontWeightProperty;
		((AvaloniaObject)fAFontIcon)[val] = ((AvaloniaObject)fis)[!(AvaloniaProperty)(object)TextElement.FontWeightProperty];
		IndexerDescriptor val2 = !(AvaloniaProperty)(object)TextElement.FontStyleProperty;
		((AvaloniaObject)fAFontIcon)[val2] = ((AvaloniaObject)fis)[!(AvaloniaProperty)(object)TextElement.FontStyleProperty];
		IndexerDescriptor val3 = !(AvaloniaProperty)(object)TextElement.FontFamilyProperty;
		((AvaloniaObject)fAFontIcon)[val3] = ((AvaloniaObject)fis)[!(AvaloniaProperty)(object)TextElement.FontFamilyProperty];
		IndexerDescriptor val4 = !(AvaloniaProperty)(object)TextElement.FontSizeProperty;
		((AvaloniaObject)fAFontIcon)[val4] = ((AvaloniaObject)fis)[!(AvaloniaProperty)(object)TextElement.FontSizeProperty];
		IndexerDescriptor val5 = !(AvaloniaProperty)(object)FAFontIcon.GlyphProperty;
		((AvaloniaObject)fAFontIcon)[val5] = ((AvaloniaObject)fis)[!(AvaloniaProperty)(object)FAFontIconSource.GlyphProperty];
		FAFontIcon fAFontIcon2 = fAFontIcon;
		if (((AvaloniaObject)fis).IsSet((AvaloniaProperty)(object)FAIconSource.ForegroundProperty))
		{
			((AvaloniaObject)fAFontIcon2).Bind<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty, AvaloniaObjectExtensions.GetBindingObservable<IBrush>((AvaloniaObject)(object)fis, (AvaloniaProperty<IBrush>)(object)FAIconSource.ForegroundProperty), (BindingPriority)0);
		}
		else
		{
			((AvaloniaObject)fAFontIcon2).Bind<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty, AvaloniaObjectExtensions.GetBindingObservable<IBrush>((AvaloniaObject)(object)fis, (AvaloniaProperty<IBrush>)(object)FAIconSource.ForegroundProperty).Skip(1), (BindingPriority)0);
		}
		return fAFontIcon2;
	}

	internal static FAPathIcon CreatePathIconFromPathIconSource(FAPathIconSource pis)
	{
		FAPathIcon fAPathIcon = new FAPathIcon();
		IndexerDescriptor val = !(AvaloniaProperty)(object)FAPathIcon.DataProperty;
		((AvaloniaObject)fAPathIcon)[val] = ((AvaloniaObject)pis)[!(AvaloniaProperty)(object)FAPathIconSource.DataProperty];
		IndexerDescriptor val2 = !(AvaloniaProperty)(object)FAPathIcon.StretchProperty;
		((AvaloniaObject)fAPathIcon)[val2] = ((AvaloniaObject)pis)[!(AvaloniaProperty)(object)FAPathIconSource.StretchProperty];
		IndexerDescriptor val3 = !(AvaloniaProperty)(object)FAPathIcon.StretchDirectionProperty;
		((AvaloniaObject)fAPathIcon)[val3] = ((AvaloniaObject)pis)[!(AvaloniaProperty)(object)FAPathIconSource.StretchDirectionProperty];
		FAPathIcon fAPathIcon2 = fAPathIcon;
		if (((AvaloniaObject)pis).IsSet((AvaloniaProperty)(object)FAIconSource.ForegroundProperty))
		{
			((AvaloniaObject)fAPathIcon2).Bind<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty, AvaloniaObjectExtensions.GetBindingObservable<IBrush>((AvaloniaObject)(object)pis, (AvaloniaProperty<IBrush>)(object)FAIconSource.ForegroundProperty), (BindingPriority)0);
		}
		else
		{
			((AvaloniaObject)fAPathIcon2).Bind<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty, AvaloniaObjectExtensions.GetBindingObservable<IBrush>((AvaloniaObject)(object)pis, (AvaloniaProperty<IBrush>)(object)FAIconSource.ForegroundProperty).Skip(1), (BindingPriority)0);
		}
		return fAPathIcon2;
	}

	internal static FASymbolIcon CreateSymbolIconFromSymbolIconSource(FASymbolIconSource sis)
	{
		FASymbolIcon fASymbolIcon = new FASymbolIcon();
		IndexerDescriptor val = !(AvaloniaProperty)(object)FASymbolIcon.SymbolProperty;
		((AvaloniaObject)fASymbolIcon)[val] = ((AvaloniaObject)sis)[!(AvaloniaProperty)(object)FASymbolIconSource.SymbolProperty];
		IndexerDescriptor val2 = !(AvaloniaProperty)(object)TextElement.FontSizeProperty;
		((AvaloniaObject)fASymbolIcon)[val2] = ((AvaloniaObject)sis)[!(AvaloniaProperty)(object)TextElement.FontSizeProperty];
		FASymbolIcon fASymbolIcon2 = fASymbolIcon;
		if (((AvaloniaObject)sis).IsSet((AvaloniaProperty)(object)FAIconSource.ForegroundProperty))
		{
			((AvaloniaObject)fASymbolIcon2).Bind<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty, AvaloniaObjectExtensions.GetBindingObservable<IBrush>((AvaloniaObject)(object)sis, (AvaloniaProperty<IBrush>)(object)FAIconSource.ForegroundProperty), (BindingPriority)0);
		}
		else
		{
			((AvaloniaObject)fASymbolIcon2).Bind<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty, AvaloniaObjectExtensions.GetBindingObservable<IBrush>((AvaloniaObject)(object)sis, (AvaloniaProperty<IBrush>)(object)FAIconSource.ForegroundProperty).Skip(1), (BindingPriority)0);
		}
		return fASymbolIcon2;
	}

	internal static FABitmapIcon CreateBitmapIconFromBitmapIconSource(FABitmapIconSource bis)
	{
		FABitmapIcon fABitmapIcon = new FABitmapIcon();
		fABitmapIcon.LinkToBitmapIconSource(bis);
		if (((AvaloniaObject)bis).IsSet((AvaloniaProperty)(object)FAIconSource.ForegroundProperty))
		{
			((AvaloniaObject)fABitmapIcon).Bind<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty, AvaloniaObjectExtensions.GetBindingObservable<IBrush>((AvaloniaObject)(object)bis, (AvaloniaProperty<IBrush>)(object)FAIconSource.ForegroundProperty), (BindingPriority)0);
		}
		else
		{
			((AvaloniaObject)fABitmapIcon).Bind<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty, AvaloniaObjectExtensions.GetBindingObservable<IBrush>((AvaloniaObject)(object)bis, (AvaloniaProperty<IBrush>)(object)FAIconSource.ForegroundProperty).Skip(1), (BindingPriority)0);
		}
		return fABitmapIcon;
	}

	internal static FAImageIcon CreateImageIconFromImageIconSource(FAImageIconSource iis)
	{
		FAImageIcon fAImageIcon = new FAImageIcon();
		IndexerDescriptor val = !(AvaloniaProperty)(object)FAImageIcon.SourceProperty;
		((AvaloniaObject)fAImageIcon)[val] = ((AvaloniaObject)iis)[!(AvaloniaProperty)(object)FAImageIconSource.SourceProperty];
		FAImageIcon fAImageIcon2 = fAImageIcon;
		if (((AvaloniaObject)iis).IsSet((AvaloniaProperty)(object)FAIconSource.ForegroundProperty))
		{
			((AvaloniaObject)fAImageIcon2).Bind<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty, AvaloniaObjectExtensions.GetBindingObservable<IBrush>((AvaloniaObject)(object)iis, (AvaloniaProperty<IBrush>)(object)FAIconSource.ForegroundProperty), (BindingPriority)0);
		}
		else
		{
			((AvaloniaObject)fAImageIcon2).Bind<IBrush>((StyledProperty<IBrush>)(object)TextElement.ForegroundProperty, AvaloniaObjectExtensions.GetBindingObservable<IBrush>((AvaloniaObject)(object)iis, (AvaloniaProperty<IBrush>)(object)FAIconSource.ForegroundProperty).Skip(1), (BindingPriority)0);
		}
		return fAImageIcon2;
	}

	internal static FAIconElement CreateFromUnknown(FAIconSource src)
	{
		if (src is FABitmapIconSource bis)
		{
			return CreateBitmapIconFromBitmapIconSource(bis);
		}
		if (src is FAFontIconSource fis)
		{
			return CreateFontIconFromFontIconSource(fis);
		}
		if (src is FAPathIconSource pis)
		{
			return CreatePathIconFromPathIconSource(pis);
		}
		if (src is FASymbolIconSource sis)
		{
			return CreateSymbolIconFromSymbolIconSource(sis);
		}
		if (src is FAImageIconSource iis)
		{
			return CreateImageIconFromImageIconSource(iis);
		}
		if (_customConverters != null)
		{
			Type type = ((object)src).GetType();
			if (_customConverters.TryGetValue(type, out var value))
			{
				return value(src);
			}
		}
		return null;
	}

	/// <summary>
	/// Registers a <see cref="T:FluentAvalonia.UI.Controls.FAIconElement" /> creation factory for custom <see cref="T:FluentAvalonia.UI.Controls.FAIconSource" /> types
	/// </summary>
	/// <remarks>
	/// When creating a custom IconSource, you will also need to create a matching FAIconElement type that
	/// will actually be used for display. Just as the built-in icons do, you will need to handle the mapping
	/// between the custom IconSource and related FAIconElement.
	/// </remarks>
	public static void RegisterCustomIconSourceFactory(Type typeOfIconSource, Func<FAIconSource, FAIconElement> factory)
	{
		if (_customConverters == null)
		{
			_customConverters = new Dictionary<Type, Func<FAIconSource, FAIconElement>>();
		}
		_customConverters.Add(typeOfIconSource, factory);
	}
}
