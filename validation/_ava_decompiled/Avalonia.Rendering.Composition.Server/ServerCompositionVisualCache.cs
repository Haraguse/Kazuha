using System;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositionVisualCache
{
	private class DirtyRectCollectorProxy(ServerCompositionVisualCache parent) : IDirtyRectCollector
	{
		public void AddRect(LtrbRect rect)
		{
			parent._needToFinalizeFrame = true;
			parent._dirtyRectTracker.AddRect(new LtrbRect((rect.Left + parent._drawAtOffset.X) * parent._scaleX, (rect.Top + parent._drawAtOffset.Y) * parent._scaleY, (rect.Right + parent._drawAtOffset.X) * parent._scaleX, (rect.Bottom + parent._drawAtOffset.Y) * parent._scaleY));
		}
	}

	private readonly ServerCompositionBitmapCache _cacheMode;

	private bool _needsFullReRender;

	private IDrawingContextLayerImpl? _layer;

	private IPlatformRenderInterfaceContext? _layerCreatedWithContext;

	private bool _layerHasTextAntialiasing;

	private PixelSize _desiredLayerSize;

	private double _scaleX;

	private double _scaleY;

	private Vector _drawAtOffset;

	private bool _needToFinalizeFrame = true;

	private readonly IDirtyRectTracker _dirtyRectTracker = new SingleDirtyRectTracker();

	public IDirtyRectCollector DirtyRectCollector { get; private set; }

	public bool IsDirty => !_dirtyRectTracker.IsEmpty;

	public ServerCompositionVisual TargetVisual { get; }

	private ServerCompositor Compositor => TargetVisual.Compositor;

	private double RenderAtScale => _cacheMode.RenderAtScale;

	private bool SnapsToDevicePixels => _cacheMode.SnapsToDevicePixels;

	private bool EnableClearType => _cacheMode.EnableClearType;

	public ServerCompositionVisualCache(ServerCompositionVisual visual, ServerCompositionBitmapCache cacheMode)
	{
		_cacheMode = cacheMode;
		TargetVisual = visual;
		DirtyRectCollector = new DirtyRectCollectorProxy(this);
		MarkForFullReRender();
	}

	public void FreeResources()
	{
		_layer?.Dispose();
		_layerCreatedWithContext = null;
	}

	public void InvalidateProperties()
	{
		MarkForFullReRender();
	}

	private void ResetDirtyRects()
	{
		_needToFinalizeFrame = true;
		_dirtyRectTracker.Initialize(LtrbRect.Infinite);
	}

	private void MarkForFullReRender()
	{
		_needsFullReRender = true;
		ResetDirtyRects();
	}

	private static bool IsCloseReal(double a, double b)
	{
		return Math.Abs((a - b) / ((b == 0.0) ? 1.0 : b)) < 1.1920928955078125E-06;
	}

	private bool UpdateRealizationDimensions()
	{
		ServerCompositionVisual targetVisual = TargetVisual;
		if (targetVisual != null && targetVisual.Root != null)
		{
			LtrbRect? subTreeBounds = targetVisual.SubTreeBounds;
			if (subTreeBounds.HasValue)
			{
				LtrbRect valueOrDefault = subTreeBounds.GetValueOrDefault();
				double num = targetVisual.Root.Scaling * RenderAtScale;
				PixelSize pixelSize = Compositor.RenderInterface.Value.MaxOffscreenRenderTargetPixelSize ?? new PixelSize(16384, 16384);
				double num2 = valueOrDefault.Width * num;
				int num3 = (int)num2;
				if (!IsCloseReal(num2, num2))
				{
					num3++;
				}
				double num4 = valueOrDefault.Height * num;
				int num5 = (int)num4;
				if (!IsCloseReal(num4, num4))
				{
					num5++;
				}
				_scaleX = (_scaleY = num);
				if (num3 > pixelSize.Width)
				{
					_scaleX *= (double)pixelSize.Width / (double)num3;
					num3 = pixelSize.Width;
				}
				if (num5 > pixelSize.Height)
				{
					_scaleY *= (double)pixelSize.Height / (double)num5;
					num5 = pixelSize.Height;
				}
				_drawAtOffset = new Vector(0.0 - valueOrDefault.Left, 0.0 - valueOrDefault.Top);
				_desiredLayerSize = new PixelSize(num3, num5);
				return true;
			}
		}
		return false;
	}

	public (int visitedVisuals, int renderedVisuals) Draw(IDrawingContextImpl outerCanvas)
	{
		if (!TargetVisual.SubTreeBounds.HasValue)
		{
			return default((int, int));
		}
		UpdateRealizationDimensions();
		IPlatformRenderInterfaceContext value = Compositor.RenderInterface.Value;
		if (_layer == null || _layerHasTextAntialiasing != EnableClearType || _layer.PixelSize != _desiredLayerSize || _layerCreatedWithContext != value)
		{
			_layer?.Dispose();
			_layer = null;
			_layerCreatedWithContext = null;
			if (_desiredLayerSize.Width < 1 || _desiredLayerSize.Height < 1)
			{
				ResetDirtyRects();
				return default((int, int));
			}
			_layer = value.CreateOffscreenRenderTarget(_desiredLayerSize, new Vector(_scaleX, _scaleX), EnableClearType);
			_layerHasTextAntialiasing = EnableClearType;
			_layerCreatedWithContext = value;
			_needsFullReRender = true;
		}
		LtrbRect bounds = new LtrbRect(0.0, 0.0, _layer.PixelSize.Width, _layer.PixelSize.Height);
		if (_needsFullReRender)
		{
			ResetDirtyRects();
			DirtyRectCollector.AddRect(LtrbRect.Infinite);
		}
		if (_needToFinalizeFrame)
		{
			_dirtyRectTracker.FinalizeFrame(bounds);
			_needToFinalizeFrame = false;
		}
		Rect destRect = TargetVisual.SubTreeBounds.Value.ToRect();
		(int, int) result = default((int, int));
		if (!_dirtyRectTracker.IsEmpty)
		{
			using IDrawingContextImpl drawingContextImpl = _layer.CreateDrawingContext();
			using (_needsFullReRender ? null : _dirtyRectTracker.BeginDraw(drawingContextImpl))
			{
				drawingContextImpl.Clear(Colors.Transparent);
				drawingContextImpl.Transform = Matrix.CreateTranslation(_drawAtOffset) * Matrix.CreateScale(_scaleX, _scaleY);
				result = TargetVisual.Render(drawingContextImpl, _dirtyRectTracker.CombinedRect, _dirtyRectTracker, renderChildren: true, skipRootVisualTransform: false, renderingToBitmapCache: true);
			}
		}
		_needsFullReRender = false;
		Matrix transform = outerCanvas.Transform;
		if (SnapsToDevicePixels)
		{
			Rect rect = destRect.TransformToAABB(transform);
			double num = rect.Left - Math.Floor(rect.Left);
			double num2 = rect.Top - Math.Floor(rect.Top);
			outerCanvas.Transform = transform * Matrix.CreateTranslation(0.0 - num, 0.0 - num2);
		}
		outerCanvas.DrawBitmap(_layer, 1.0, new Rect(0.0, 0.0, _layer.PixelSize.Width, _layer.PixelSize.Height), destRect);
		if (SnapsToDevicePixels)
		{
			outerCanvas.Transform = transform;
		}
		ResetDirtyRects();
		return result;
	}
}
