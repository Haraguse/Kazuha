using System;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal abstract class ServerCompositionSurface : ServerObject
{
	public abstract IRef<IBitmapImpl>? Bitmap { get; }

	public Action? Changed { get; set; }

	protected ServerCompositionSurface(ServerCompositor compositor)
		: base(compositor)
	{
	}
}
