using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal sealed class ServerCompositionCustomVisual : ServerCompositionContainerVisual, IServerClockItem
{
	private readonly CompositionCustomVisualHandler _handler;

	private bool _wantsNextAnimationFrameAfterTick;

	internal ServerCompositionCustomVisual(ServerCompositor compositor, CompositionCustomVisualHandler handler)
		: base(compositor)
	{
		_handler = handler ?? throw new ArgumentNullException("handler");
		_handler.Attach(this);
	}

	public void DispatchMessages(List<object> messages)
	{
		foreach (object message in messages)
		{
			try
			{
				_handler.OnMessage(message);
			}
			catch (Exception propertyValue)
			{
				Logger.TryGet(LogEventLevel.Error, "Visual")?.Log(_handler, $"Exception in {_handler.GetType().Name}.{"OnMessage"} {{0}}", propertyValue);
			}
		}
	}

	public void OnTick()
	{
		_wantsNextAnimationFrameAfterTick = false;
		_handler.OnAnimationFrameUpdate();
		if (!_wantsNextAnimationFrameAfterTick)
		{
			base.Compositor.Animations.RemoveFromClock(this);
		}
	}

	public override LtrbRect? ComputeOwnContentBounds()
	{
		return new LtrbRect(_handler.GetRenderBounds());
	}

	protected override void OnAttachedToRoot(ServerCompositionTarget target)
	{
		if (_wantsNextAnimationFrameAfterTick)
		{
			base.Compositor.Animations.AddToClock(this);
		}
		base.OnAttachedToRoot(target);
	}

	protected override void OnDetachedFromRoot(ServerCompositionTarget target)
	{
		base.Compositor.Animations.RemoveFromClock(this);
		base.OnDetachedFromRoot(target);
	}

	internal void HandlerInvalidate()
	{
		InvalidateContent();
	}

	internal void HandlerInvalidate(Rect rc)
	{
		AddExtraDirtyRect(new LtrbRect(rc));
	}

	internal void HandlerRegisterForNextAnimationFrameUpdate()
	{
		_wantsNextAnimationFrameAfterTick = true;
		if (base.Root != null)
		{
			base.Compositor.Animations.AddToClock(this);
		}
	}

	protected override void RenderCore(ServerVisualRenderContext ctx, LtrbRect currentTransformedClip)
	{
		CompositorDrawingContextProxy compositorDrawingContextProxy = ctx.Canvas as CompositorDrawingContextProxy;
		if (compositorDrawingContextProxy != null)
		{
			compositorDrawingContextProxy.AutoFlush = true;
			compositorDrawingContextProxy.Flush();
		}
		using ImmediateDrawingContext drawingContext = new ImmediateDrawingContext(ctx.Canvas, ctx.Canvas.Transform, ownsImpl: false);
		try
		{
			_handler.Render(drawingContext, currentTransformedClip.ToRect());
		}
		catch (Exception propertyValue)
		{
			Logger.TryGet(LogEventLevel.Error, "Visual")?.Log(_handler, $"Exception in {_handler.GetType().Name}.{"OnRender"} {{0}}", propertyValue);
		}
		if (compositorDrawingContextProxy != null)
		{
			compositorDrawingContextProxy.AutoFlush = false;
		}
	}
}
