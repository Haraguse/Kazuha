using System;
using Avalonia.Rendering.Composition;

namespace Avalonia.Media;

/// <summary>
/// Represents cached content modes for graphics acceleration features.
/// </summary>
public abstract class CacheMode : StyledElement
{
	internal abstract CompositionCacheMode GetForCompositor(Compositor c);

	public static CacheMode Parse(string s)
	{
		if (s == "BitmapCache")
		{
			return new BitmapCache();
		}
		throw new ArgumentException("Unknown CacheMode: " + s);
	}
}
