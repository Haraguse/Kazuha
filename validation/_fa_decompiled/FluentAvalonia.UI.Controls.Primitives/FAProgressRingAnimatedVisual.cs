using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using Avalonia.VisualTree;
using SkiaSharp;

namespace FluentAvalonia.UI.Controls.Primitives;

/// <summary>
/// Represents the animated visual source for a <see cref="T:FluentAvalonia.UI.Controls.FAProgressRing" />
/// </summary>
/// <remarks>
/// This class is only public for Xaml support in the control template of the ProgressRing
/// </remarks>
public sealed class FAProgressRingAnimatedVisual : Control
{
	private enum HandlerMessageType
	{
		Background,
		Foreground,
		Min,
		Max,
		Value,
		Active,
		Indeterminate
	}

	private class HandlerMessage
	{
		public HandlerMessageType MessageType { get; }

		public object Data { get; }

		public HandlerMessage(HandlerMessageType type, object data)
		{
			MessageType = type;
			Data = data;
		}
	}

	private class CustomCompHandler : CompositionCustomVisualHandler
	{
		private TimeSpan? _lastTime;

		private float _duration;

		private readonly SKPaint _paint;

		private readonly SKPath _path;

		private readonly SKPaint _layerPaint;

		private readonly SKRect _visualBounds;

		private SKColor? _background;

		private SKColor _foreground;

		private float _min;

		private float _max;

		private float _value;

		private bool _indeterminate;

		private bool _active;

		private bool _isAnimatingToValue;

		private float _lastValue;

		public CustomCompHandler(double minimum, double maximum, double value, bool isActive, IBrush background, IBrush foreground)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Expected O, but got Unknown
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Expected O, but got Unknown
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Expected O, but got Unknown
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			_duration = 2f;
			_visualBounds = new SKRect(10f, 10f, 70f, 70f);
			((CompositionCustomVisualHandler)this)._002Ector();
			_min = (float)minimum;
			_max = (float)maximum;
			_value = (float)value;
			_active = isActive;
			ISolidColorBrush val = (ISolidColorBrush)(object)((background is ISolidColorBrush) ? background : null);
			if (val != null)
			{
				_background = SkiaSharpExtensions.ToSKColor(val.Color);
			}
			ISolidColorBrush val2 = (ISolidColorBrush)(object)((foreground is ISolidColorBrush) ? foreground : null);
			if (val2 != null)
			{
				_foreground = SkiaSharpExtensions.ToSKColor(val2.Color);
			}
			_paint = new SKPaint
			{
				IsAntialias = true,
				IsStroke = true,
				StrokeWidth = 4f,
				StrokeCap = (SKStrokeCap)1
			};
			_layerPaint = new SKPaint();
			_path = new SKPath();
		}

		public override void OnRender(ImmediateDrawingContext drawingContext)
		{
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			if (!_active)
			{
				return;
			}
			ISkiaSharpApiLease val = OptionalFeatureProviderExtensions.TryGetFeature<ISkiaSharpApiLeaseFeature>((IOptionalFeatureProvider)(object)drawingContext).Lease();
			try
			{
				SKCanvas skCanvas = val.SkCanvas;
				double num = Math.Clamp(val.CurrentOpacity, 0.0, 1.0);
				bool num2 = num < 1.0;
				if (num2)
				{
					_layerPaint.Color = ((SKColor)(ref SKColors.White)).WithAlpha((byte)(255.0 * num));
					skCanvas.SaveLayer(_layerPaint);
				}
				if (_background.HasValue)
				{
					_paint.Color = _background.Value;
					skCanvas.DrawArc(_visualBounds, 0f, 360f, false, _paint);
				}
				_paint.Color = _foreground;
				skCanvas.DrawPath(_path, _paint);
				if (num2)
				{
					skCanvas.Restore();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public override void OnAnimationFrameUpdate()
		{
			((CompositionCustomVisualHandler)this).Invalidate();
			Update();
			if (_active && (_indeterminate || _isAnimatingToValue))
			{
				((CompositionCustomVisualHandler)this).RegisterForNextAnimationFrameUpdate();
			}
		}

		private void Update()
		{
			//IL_0245: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_011f: Unknown result type (might be due to invalid IL or missing references)
			if (_indeterminate)
			{
				TimeSpan compositionNow = ((CompositionCustomVisualHandler)this).CompositionNow;
				if (!_lastTime.HasValue)
				{
					_lastTime = compositionNow;
				}
				double num = (compositionNow - _lastTime.Value).TotalSeconds;
				if (num > (double)_duration)
				{
					while (num > (double)_duration)
					{
						num -= (double)_duration;
					}
					_lastTime = compositionNow - TimeSpan.FromSeconds(num);
				}
				float num2 = (float)(num / (double)_duration);
				float num3 = 0f;
				float num4 = 0f;
				float num5 = 0f;
				num3 = (((double)num2 < 0.25) ? (180f * (num2 / 0.25f)) : ((!((double)num2 >= 0.75)) ? 180f : (180f * ((1f - num2) / 0.25f))));
				num4 = num3 / 2f;
				num5 = 1080f * num2;
				_path.Reset();
				_path.MoveTo(40f, 10f);
				_path.AddArc(_visualBounds, -90f + (num5 - num4), num3);
			}
			else if (_isAnimatingToValue)
			{
				TimeSpan compositionNow2 = ((CompositionCustomVisualHandler)this).CompositionNow;
				if (!_lastTime.HasValue)
				{
					_lastTime = compositionNow2;
				}
				float num6 = (float)((compositionNow2 - _lastTime.Value).TotalSeconds / (double)_duration);
				if (num6 >= 1f)
				{
					_isAnimatingToValue = false;
					_lastTime = null;
					num6 = 1f;
				}
				float num7 = _value - _lastValue;
				float num8 = _lastValue + num7 * num6;
				_path.Reset();
				_path.MoveTo(40f, 10f);
				_path.AddArc(_visualBounds, -90f, 360f * (num8 - _min) / (_max - _min));
			}
			else
			{
				_path.Reset();
				_path.MoveTo(40f, 10f);
				_path.AddArc(_visualBounds, -90f, 360f * (_value - _min) / (_max - _min));
			}
		}

		public override void OnMessage(object message)
		{
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_0179: Unknown result type (might be due to invalid IL or missing references)
			//IL_017e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0167: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Unknown result type (might be due to invalid IL or missing references)
			//IL_016f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0171: Unknown result type (might be due to invalid IL or missing references)
			if (!(message is HandlerMessage handlerMessage))
			{
				return;
			}
			switch (handlerMessage.MessageType)
			{
			case HandlerMessageType.Min:
				_min = (float)handlerMessage.Data;
				break;
			case HandlerMessageType.Max:
				_max = (float)handlerMessage.Data;
				break;
			case HandlerMessageType.Value:
			{
				float num = (float)handlerMessage.Data;
				_lastValue = _value;
				if (num <= _value)
				{
					_value = num;
					_isAnimatingToValue = false;
					break;
				}
				_value = num;
				_isAnimatingToValue = true;
				((CompositionCustomVisualHandler)this).RegisterForNextAnimationFrameUpdate();
				return;
			}
			case HandlerMessageType.Active:
				_active = (bool)handlerMessage.Data;
				if (_active && _indeterminate)
				{
					((CompositionCustomVisualHandler)this).RegisterForNextAnimationFrameUpdate();
					return;
				}
				_lastTime = null;
				break;
			case HandlerMessageType.Indeterminate:
				_indeterminate = (bool)handlerMessage.Data;
				if (_indeterminate && _active)
				{
					((CompositionCustomVisualHandler)this).RegisterForNextAnimationFrameUpdate();
					return;
				}
				_lastTime = null;
				break;
			case HandlerMessageType.Background:
				if (handlerMessage.Data is SKColor value)
				{
					_background = value;
				}
				else
				{
					_background = null;
				}
				break;
			case HandlerMessageType.Foreground:
				if (handlerMessage.Data is SKColor foreground)
				{
					_foreground = foreground;
				}
				else
				{
					_foreground = SKColors.Transparent;
				}
				break;
			}
			Update();
			((CompositionCustomVisualHandler)this).Invalidate();
		}
	}

	private CustomCompHandler _handler;

	private CompositionCustomVisual _sfc;

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		((Visual)this).OnAttachedToVisualTree(e);
		FAProgressRing fAProgressRing = VisualExtensions.FindAncestorOfType<FAProgressRing>((Visual)(object)this, false);
		bool isIndeterminate = fAProgressRing.IsIndeterminate;
		_handler = new CustomCompHandler(((RangeBase)fAProgressRing).Minimum, ((RangeBase)fAProgressRing).Maximum, ((RangeBase)fAProgressRing).Value, fAProgressRing.IsActive, ((TemplatedControl)fAProgressRing).Background, ((TemplatedControl)fAProgressRing).Foreground);
		if (_sfc == null)
		{
			Compositor compositor = ((CompositionObject)ElementComposition.GetElementVisual((Visual)(object)this)).Compositor;
			_sfc = compositor.CreateCustomVisual((CompositionCustomVisualHandler)(object)_handler);
			((CompositionVisual)_sfc).Size = new Vector(80.0, 80.0);
			ElementComposition.SetElementChildVisual((Visual)(object)this, (CompositionVisual)(object)_sfc);
		}
		_sfc.SendHandlerMessage((object)new HandlerMessage(HandlerMessageType.Indeterminate, isIndeterminate));
	}

	protected override void OnSizeChanged(SizeChangedEventArgs e)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		((Control)this).OnSizeChanged(e);
		Size newSize = e.NewSize;
		double width = ((Size)(ref newSize)).Width;
		newSize = e.NewSize;
		double num = Math.Min(width, ((Size)(ref newSize)).Height);
		((CompositionVisual)_sfc).Scale = new Vector3D(num / 80.0, num / 80.0, 1.0);
	}

	internal void SetMinimum(double min)
	{
		CompositionCustomVisual sfc = _sfc;
		if (sfc != null)
		{
			sfc.SendHandlerMessage((object)new HandlerMessage(HandlerMessageType.Min, (float)min));
		}
	}

	internal void SetMaximum(double max)
	{
		CompositionCustomVisual sfc = _sfc;
		if (sfc != null)
		{
			sfc.SendHandlerMessage((object)new HandlerMessage(HandlerMessageType.Max, (float)max));
		}
	}

	internal void SetValue(double val)
	{
		CompositionCustomVisual sfc = _sfc;
		if (sfc != null)
		{
			sfc.SendHandlerMessage((object)new HandlerMessage(HandlerMessageType.Value, (float)val));
		}
	}

	internal void SetActive(bool active)
	{
		CompositionCustomVisual sfc = _sfc;
		if (sfc != null)
		{
			sfc.SendHandlerMessage((object)new HandlerMessage(HandlerMessageType.Active, active));
		}
	}

	internal void SetIndeterminate(bool indeterminate)
	{
		CompositionCustomVisual sfc = _sfc;
		if (sfc != null)
		{
			sfc.SendHandlerMessage((object)new HandlerMessage(HandlerMessageType.Indeterminate, indeterminate));
		}
	}

	internal void SetBackground(IBrush brush)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		ISolidColorBrush val = (ISolidColorBrush)(object)((brush is ISolidColorBrush) ? brush : null);
		if (val != null)
		{
			CompositionCustomVisual sfc = _sfc;
			if (sfc != null)
			{
				sfc.SendHandlerMessage((object)new HandlerMessage(HandlerMessageType.Background, SkiaSharpExtensions.ToSKColor(val.Color)));
			}
		}
		else
		{
			CompositionCustomVisual sfc2 = _sfc;
			if (sfc2 != null)
			{
				sfc2.SendHandlerMessage((object)new HandlerMessage(HandlerMessageType.Background, null));
			}
		}
	}

	internal void SetForeground(IBrush brush)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		ISolidColorBrush val = (ISolidColorBrush)(object)((brush is ISolidColorBrush) ? brush : null);
		if (val != null)
		{
			CompositionCustomVisual sfc = _sfc;
			if (sfc != null)
			{
				sfc.SendHandlerMessage((object)new HandlerMessage(HandlerMessageType.Foreground, SkiaSharpExtensions.ToSKColor(val.Color)));
			}
		}
		else
		{
			CompositionCustomVisual sfc2 = _sfc;
			if (sfc2 != null)
			{
				sfc2.SendHandlerMessage((object)new HandlerMessage(HandlerMessageType.Foreground, SKColors.Transparent));
			}
		}
	}
}
