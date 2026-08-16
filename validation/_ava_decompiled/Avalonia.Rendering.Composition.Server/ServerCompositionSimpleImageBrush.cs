using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal sealed class ServerCompositionSimpleImageBrush : ServerCompositionSimpleTileBrush, IImageBrush, ITileBrush, IBrush, IImageBrushSource
{
	public IImageBrushSource? Source => this;

	public IRef<IBitmapImpl>? Bitmap { get; private set; }

	internal ServerCompositionSimpleImageBrush(ServerCompositor compositor)
		: base(compositor)
	{
	}

	public override void Dispose()
	{
		Bitmap?.Dispose();
		Bitmap = null;
		base.Dispose();
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		Bitmap?.Dispose();
		Bitmap = null;
		Bitmap = reader.ReadObject<IRef<IBitmapImpl>>();
	}
}
