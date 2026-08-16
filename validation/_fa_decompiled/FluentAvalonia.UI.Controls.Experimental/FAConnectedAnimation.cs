using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.VisualTree;
using FluentAvalonia.Core.Attributes;

namespace FluentAvalonia.UI.Controls.Experimental;

public class FAConnectedAnimation
{
	private FAConnectedAnimationService _owningService;

	private Visual _sourceElement;

	private float _initialOpacity;

	private Vector3 _initialOffset;

	private Vector2 _initialSize;

	public FAConnectedAnimationConfiguration Configuration { get; set; }

	public bool IsScaleAnimationEnabled { get; set; }

	internal DateTime CreationTime { get; }

	internal FAConnectedAnimation(Visual source, FAConnectedAnimationService service)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		base._002Ector();
		Configuration = new FAGravityConnectedAnimationConfiguration();
		IsScaleAnimationEnabled = true;
		_owningService = service;
		_sourceElement = source;
		CompositionVisual elementVisual = ElementComposition.GetElementVisual(source);
		if (elementVisual == null)
		{
			throw new InvalidOperationException("No CompositionVisual found for source element");
		}
		TransformedBounds? transformedBounds = VisualExtensions.GetTransformedBounds(source);
		TransformedBounds value = transformedBounds.Value;
		Rect bounds = ((TransformedBounds)(ref value)).Bounds;
		value = transformedBounds.Value;
		Rect val = ((Rect)(ref bounds)).TransformToAABB(((TransformedBounds)(ref value)).Transform);
		_initialSize = new Vector2((float)((Rect)(ref val)).Width, (float)((Rect)(ref val)).Height);
		_initialOffset = new Vector3((float)((Rect)(ref val)).X, (float)((Rect)(ref val)).Y, 0f);
		_initialOpacity = elementVisual.Opacity;
		CreationTime = DateTime.Now;
	}

	[FANotImplemented]
	public void Cancel()
	{
	}

	public bool TryStart(Visual destination)
	{
		return TryStart(destination, null);
	}

	public bool TryStart(Visual destination, IList<Visual> coordinatedVisuals)
	{
		if (_sourceElement == destination)
		{
			return false;
		}
		if ((DateTime.Now - CreationTime).TotalMilliseconds > 300.0)
		{
			return false;
		}
		ConfigureConnectedAnimation(destination, coordinatedVisuals);
		return true;
	}

	private void ConfigureConnectedAnimation(Visual destination, IList<Visual> coordinatedVisuals = null)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		CompositionVisual elementVisual = ElementComposition.GetElementVisual(destination);
		if (elementVisual == null)
		{
			throw new Exception("No composition visual found for destination element");
		}
		Easing val = (Easing)((!(Configuration is FAGravityConnectedAnimationConfiguration)) ? ((object)_owningService.DefaultEasingFunction) : ((object)new QuarticEaseInOut()));
		TimeSpan duration = ((Configuration is FAGravityConnectedAnimationConfiguration) ? TimeSpan.FromMilliseconds(750L) : _owningService.DefaultDuration);
		Compositor compositor = ((CompositionObject)elementVisual).Compositor;
		TransformedBounds? transformedBounds = VisualExtensions.GetTransformedBounds(destination);
		TransformedBounds value = transformedBounds.Value;
		Rect bounds = ((TransformedBounds)(ref value)).Bounds;
		value = transformedBounds.Value;
		Rect val2 = ((Rect)(ref bounds)).TransformToAABB(((TransformedBounds)(ref value)).Transform);
		Vector2 vector = new Vector2((float)((Rect)(ref val2)).Width, (float)((Rect)(ref val2)).Height);
		Vector3 vector2 = new Vector3((float)((Rect)(ref val2)).X, (float)((Rect)(ref val2)).Y, 0f);
		Vector3 vector3 = _initialOffset - vector2;
		CompositionAnimationGroup val3 = compositor.CreateAnimationGroup();
		if (_initialOpacity != elementVisual.Opacity)
		{
			float opacity = elementVisual.Opacity;
			ScalarKeyFrameAnimation val4 = compositor.CreateScalarKeyFrameAnimation();
			((CompositionAnimation)val4).Target = "Opacity";
			((KeyFrameAnimation)val4).Duration = duration;
			((CompositionAnimation)val4).SetScalarParameter("StartValue", _initialOpacity);
			((CompositionAnimation)val4).SetScalarParameter("FinalValue", opacity);
			((KeyFrameAnimation)val4).InsertExpressionKeyFrame(0f, "StartValue", (Easing)null);
			((KeyFrameAnimation)val4).InsertExpressionKeyFrame(1f, "FinalValue", val);
			val3.Add((CompositionAnimation)(object)val4);
		}
		Vector3KeyFrameAnimation val5 = compositor.CreateVector3KeyFrameAnimation();
		((CompositionAnimation)val5).Target = "Offset";
		((KeyFrameAnimation)val5).Duration = duration;
		Vector3D offset = elementVisual.Offset;
		float x = (float)((Vector3D)(ref offset)).X;
		offset = elementVisual.Offset;
		float y = (float)((Vector3D)(ref offset)).Y;
		offset = elementVisual.Offset;
		Vector3 vector4 = new Vector3(x, y, (float)((Vector3D)(ref offset)).Z);
		((CompositionAnimation)val5).SetVector3Parameter("StartValue", vector4 + vector3);
		((CompositionAnimation)val5).SetVector3Parameter("FinalValue", vector4);
		((KeyFrameAnimation)val5).InsertExpressionKeyFrame(0f, "StartValue", (Easing)null);
		if (Configuration is FAGravityConnectedAnimationConfiguration)
		{
			((CompositionAnimation)val5).SetScalarParameter("Gravity", MathF.Abs(vector2.Y - _initialOffset.Y) * 0.1f);
			((KeyFrameAnimation)val5).InsertExpressionKeyFrame(0.1f, "Vector3(StartValue.X, StartValue.Y + Gravity, 0)", (Easing)null);
		}
		((KeyFrameAnimation)val5).InsertExpressionKeyFrame(1f, "FinalValue", val);
		val3.Add((CompositionAnimation)(object)val5);
		if (IsScaleAnimationEnabled)
		{
			float x2 = _initialSize.X / vector.X;
			float y2 = _initialSize.Y / vector.Y;
			Vector3KeyFrameAnimation val6 = compositor.CreateVector3KeyFrameAnimation();
			((CompositionAnimation)val6).Target = "Scale";
			((KeyFrameAnimation)val6).Duration = duration;
			((CompositionAnimation)val6).SetVector3Parameter("StartValue", new Vector3(x2, y2, 1f));
			((CompositionAnimation)val6).SetVector3Parameter("FinalValue", new Vector3(1f, 1f, 1f));
			((KeyFrameAnimation)val6).InsertExpressionKeyFrame(0f, "StartValue", (Easing)null);
			((KeyFrameAnimation)val6).InsertExpressionKeyFrame(1f, "FinalValue", val);
			val3.Add((CompositionAnimation)(object)val6);
		}
		if (coordinatedVisuals != null)
		{
			for (int i = 0; i < coordinatedVisuals.Count; i++)
			{
				CreateCoordinatedAnimation(coordinatedVisuals[i], duration, val);
			}
		}
		((CompositionObject)elementVisual).StartAnimationGroup((ICompositionAnimationBase)(object)val3);
	}

	private void CreateCoordinatedAnimation(Visual element, TimeSpan duration, Easing easing)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		CompositionVisual elementVisual = ElementComposition.GetElementVisual(element);
		if (elementVisual != null)
		{
			Compositor compositor = ((CompositionObject)elementVisual).Compositor;
			TransformedBounds? transformedBounds = VisualExtensions.GetTransformedBounds(element);
			TransformedBounds value = transformedBounds.Value;
			Rect bounds = ((TransformedBounds)(ref value)).Bounds;
			value = transformedBounds.Value;
			Rect val = ((Rect)(ref bounds)).TransformToAABB(((TransformedBounds)(ref value)).Transform);
			Vector3 vector = new Vector3((float)((Rect)(ref val)).X, (float)((Rect)(ref val)).Y, 0f);
			Vector3 vector2 = _initialOffset - vector;
			CompositionAnimationGroup val2 = compositor.CreateAnimationGroup();
			float opacity = elementVisual.Opacity;
			ScalarKeyFrameAnimation val3 = compositor.CreateScalarKeyFrameAnimation();
			((CompositionAnimation)val3).Target = "Opacity";
			((KeyFrameAnimation)val3).Duration = duration;
			((KeyFrameAnimation)val3).DelayBehavior = (AnimationDelayBehavior)1;
			((CompositionAnimation)val3).SetScalarParameter("StartValue", 0f);
			((CompositionAnimation)val3).SetScalarParameter("FinalValue", opacity);
			((KeyFrameAnimation)val3).InsertExpressionKeyFrame(0f, "StartValue", (Easing)null);
			((KeyFrameAnimation)val3).InsertExpressionKeyFrame(0.5f, "StartValue", (Easing)null);
			((KeyFrameAnimation)val3).InsertExpressionKeyFrame(1f, "FinalValue", easing);
			val2.Add((CompositionAnimation)(object)val3);
			Vector3KeyFrameAnimation val4 = compositor.CreateVector3KeyFrameAnimation();
			Vector3D offset = elementVisual.Offset;
			float x = (float)((Vector3D)(ref offset)).X;
			offset = elementVisual.Offset;
			float y = (float)((Vector3D)(ref offset)).Y;
			offset = elementVisual.Offset;
			Vector3 vector3 = new Vector3(x, y, (float)((Vector3D)(ref offset)).Z);
			((CompositionAnimation)val4).Target = "Offset";
			((KeyFrameAnimation)val4).Duration = duration;
			((CompositionAnimation)val4).SetVector3Parameter("StartValue", vector3 + vector2);
			((CompositionAnimation)val4).SetVector3Parameter("FinalValue", vector3);
			((KeyFrameAnimation)val4).InsertExpressionKeyFrame(0f, "StartValue", (Easing)null);
			((KeyFrameAnimation)val4).InsertExpressionKeyFrame(1f, "FinalValue", easing);
			val2.Add((CompositionAnimation)(object)val4);
			((CompositionObject)elementVisual).StartAnimationGroup((ICompositionAnimationBase)(object)val2);
		}
	}
}
