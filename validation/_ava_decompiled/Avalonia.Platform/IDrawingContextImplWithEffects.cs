using System;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Platform;

[PrivateApi]
public interface IDrawingContextImplWithEffects : IDrawingContextImpl, IDisposable
{
	void PushEffect(Rect? clipRect, IEffect effect);

	void PopEffect();
}
