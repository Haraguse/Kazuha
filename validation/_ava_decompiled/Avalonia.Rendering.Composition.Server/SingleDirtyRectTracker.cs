using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Rendering.Composition.Server;

internal class SingleDirtyRectTracker : IDirtyRectTracker, IDirtyRectCollector
{
	private LtrbRect? _rect;

	private LtrbRect _extendedRect;

	private readonly Random _random = new Random();

	public bool IsEmpty => _rect?.IsZeroSize ?? true;

	public LtrbRect CombinedRect => _extendedRect;

	public void AddRect(LtrbRect rect)
	{
		_rect = LtrbRect.FullUnion(_rect, rect);
	}

	public void FinalizeFrame(LtrbRect bounds)
	{
		_extendedRect = (_rect.HasValue ? LtrbPixelRect.FromRectUnscaled(_rect.Value.Inflate(new Thickness(1.0)).IntersectOrEmpty(bounds)).ToLtrbRectUnscaled() : default(LtrbRect));
	}

	public IDisposable BeginDraw(IDrawingContextImpl ctx)
	{
		ctx.PushClip(_extendedRect.ToRect());
		return Disposable.Create(ctx.PopClip);
	}

	public bool Intersects(LtrbRect rect)
	{
		return _extendedRect.Intersects(rect);
	}

	public void Initialize(LtrbRect bounds)
	{
		_rect = null;
	}

	public void Visualize(IDrawingContextImpl context)
	{
		context.DrawRectangle(new ImmutableSolidColorBrush(new Color(30, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))), null, _extendedRect.ToRect());
	}
}
