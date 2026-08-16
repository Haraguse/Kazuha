using System;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media;

internal class ImmutableGlyphRunReference : IImmutableGlyphRunReference, IDisposable
{
	public IRef<IGlyphRunImpl>? GlyphRun { get; private set; }

	public ImmutableGlyphRunReference(IRef<IGlyphRunImpl>? glyphRun)
	{
		GlyphRun = glyphRun;
	}

	public void Dispose()
	{
		GlyphRun?.Dispose();
		GlyphRun = null;
	}
}
