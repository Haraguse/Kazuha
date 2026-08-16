using System;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media;

public interface IImmutableGlyphRunReference : IDisposable
{
	internal IRef<IGlyphRunImpl>? GlyphRun { get; }
}
