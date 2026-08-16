using System;
using Avalonia.Platform;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// A class that is responsible for text shaping.
/// </summary>
public class TextShaper
{
	private readonly ITextShaperImpl _platformImpl;

	/// <summary>
	/// Gets the current text shaper.
	/// </summary>
	public static TextShaper Current
	{
		get
		{
			TextShaper service = AvaloniaLocator.Current.GetService<TextShaper>();
			if (service != null)
			{
				return service;
			}
			service = new TextShaper(AvaloniaLocator.Current.GetRequiredService<ITextShaperImpl>());
			AvaloniaLocator.CurrentMutable.Bind<TextShaper>().ToConstant(service);
			return service;
		}
	}

	public TextShaper(ITextShaperImpl platformImpl)
	{
		_platformImpl = platformImpl;
	}

	/// <inheritdoc cref="M:Avalonia.Platform.ITextShaperImpl.ShapeText(System.ReadOnlyMemory{System.Char},Avalonia.Media.TextFormatting.TextShaperOptions)" />
	public ShapedBuffer ShapeText(ReadOnlyMemory<char> text, TextShaperOptions options = default(TextShaperOptions))
	{
		return _platformImpl.ShapeText(text, options);
	}

	public ShapedBuffer ShapeText(string text, TextShaperOptions options = default(TextShaperOptions))
	{
		return ShapeText(text.AsMemory(), options);
	}
}
