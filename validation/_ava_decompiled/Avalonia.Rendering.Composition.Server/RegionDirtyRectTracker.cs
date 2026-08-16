using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Rendering.Composition.Server;

internal class RegionDirtyRectTracker : IDirtyRectTracker, IDirtyRectCollector
{
	private readonly IPlatformRenderInterfaceRegion _region;

	private readonly List<LtrbRect> _rects = new List<LtrbRect>();

	private Random _random = new Random();

	public bool IsEmpty => _rects.Count == 0;

	public LtrbRect CombinedRect { get; private set; }

	public RegionDirtyRectTracker(IPlatformRenderInterface platformRender)
	{
		_region = platformRender.CreateRegion();
	}

	public void AddRect(LtrbRect rect)
	{
		_rects.Add(rect);
	}

	private LtrbPixelRect GetInflatedPixelRect(LtrbRect rc)
	{
		return LtrbPixelRect.FromRectUnscaled(rc.Inflate(new Thickness(1.0)).IntersectOrEmpty(rc));
	}

	public void FinalizeFrame(LtrbRect bounds)
	{
		_region.Reset();
		foreach (LtrbRect rect in _rects)
		{
			_region.AddRect(GetInflatedPixelRect(rect));
		}
		CombinedRect = _region.Bounds.ToLtrbRectUnscaled();
	}

	public IDisposable BeginDraw(IDrawingContextImpl ctx)
	{
		ctx.PushClip(_region);
		return Disposable.Create(ctx.PopClip);
	}

	public bool Intersects(LtrbRect rect)
	{
		return _region.Intersects(rect);
	}

	public void Initialize(LtrbRect bounds)
	{
		_rects.Clear();
	}

	public void Visualize(IDrawingContextImpl context)
	{
		context.DrawRegion(new ImmutableSolidColorBrush(new Color(150, (byte)_random.Next(255), (byte)_random.Next(255), (byte)_random.Next(255))), null, _region);
	}
}
