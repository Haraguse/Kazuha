using System;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionSurfaceVisual : ServerSizeDependantVisual
{
	private ServerCompositionSurface? _surface;

	internal static readonly CompositionProperty<ServerCompositionSurface?> s_IdOfSurfaceProperty = CompositionProperty.Register<ServerCompositionSurfaceVisual, ServerCompositionSurface>("Surface", (SimpleServerObject obj) => ((ServerCompositionSurfaceVisual)obj)._surface, delegate(SimpleServerObject obj, ServerCompositionSurface? v)
	{
		((ServerCompositionSurfaceVisual)obj)._surface = v;
	}, null);

	public ServerCompositionSurface? Surface
	{
		get
		{
			return _surface;
		}
		set
		{
			bool flag = false;
			if (_surface != value)
			{
				OnSurfaceChanging();
				flag = true;
			}
			SetValue(s_IdOfSurfaceProperty, ref _surface, value);
			if (flag)
			{
				OnSurfaceChanged();
			}
		}
	}

	protected override void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
	{
		if (Surface != null && Surface.Bitmap != null)
		{
			IBitmapImpl item = Surface.Bitmap.Item;
			context.Canvas.DrawBitmap(Surface.Bitmap.Item, 1.0, new Rect(item.PixelSize.ToSize(1.0)), new Rect(new Size(base.Size.X, base.Size.Y)));
		}
	}

	private void OnSurfaceInvalidated()
	{
		InvalidateContent();
	}

	protected override void OnAttachedToRoot(ServerCompositionTarget target)
	{
		if (Surface != null)
		{
			ServerCompositionSurface? surface = Surface;
			surface.Changed = (Action)Delegate.Combine(surface.Changed, new Action(OnSurfaceInvalidated));
		}
		base.OnAttachedToRoot(target);
	}

	protected override void OnDetachedFromRoot(ServerCompositionTarget target)
	{
		if (Surface != null)
		{
			ServerCompositionSurface? surface = Surface;
			surface.Changed = (Action)Delegate.Remove(surface.Changed, new Action(OnSurfaceInvalidated));
		}
		base.OnDetachedFromRoot(target);
	}

	internal ServerCompositionSurfaceVisual(ServerCompositor compositor)
		: base(compositor)
	{
	}

	private void OnSurfaceChanged()
	{
		if (Surface != null)
		{
			ServerCompositionSurface? surface = Surface;
			surface.Changed = (Action)Delegate.Combine(surface.Changed, new Action(OnSurfaceInvalidated));
		}
	}

	private void OnSurfaceChanging()
	{
		if (Surface != null)
		{
			ServerCompositionSurface? surface = Surface;
			surface.Changed = (Action)Delegate.Remove(surface.Changed, new Action(OnSurfaceInvalidated));
		}
	}

	protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
	{
		base.DeserializeChangesCore(reader, committedAt);
		if ((reader.Read<CompositionSurfaceVisualChangedFields>() & CompositionSurfaceVisualChangedFields.Surface) == CompositionSurfaceVisualChangedFields.Surface)
		{
			Surface = reader.ReadObject<ServerCompositionSurface>();
		}
	}
}
