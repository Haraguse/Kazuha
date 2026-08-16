using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls.Experimental;

public class FAConnectedAnimationService
{
	internal static readonly AttachedProperty<FAConnectedAnimationService> ConnectedAnimationServiceProperty = AvaloniaProperty.RegisterAttached<FAConnectedAnimationService, TopLevel, FAConnectedAnimationService>("FAConnectedAnimationService", (FAConnectedAnimationService)null, false, (BindingMode)1, (Func<FAConnectedAnimationService, bool>)null, (Func<AvaloniaObject, FAConnectedAnimationService, FAConnectedAnimationService>)null);

	private WeakReference<TopLevel> _topLevel;

	private Dictionary<string, FAConnectedAnimation> _animations;

	public TimeSpan DefaultDuration { get; set; }

	public Easing DefaultEasingFunction { get; set; }

	internal FAConnectedAnimationService(TopLevel topLevel)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		DefaultDuration = TimeSpan.FromMilliseconds(3000L);
		DefaultEasingFunction = (Easing)new SplineEasing(0.8, 0.0, 0.2, 1.0);
		base._002Ector();
		_topLevel = new WeakReference<TopLevel>(topLevel);
		_animations = new Dictionary<string, FAConnectedAnimation>();
		DefaultDuration = TimeSpan.FromMilliseconds(300L);
		DefaultEasingFunction = (Easing)new SplineEasing(0.8, 0.0, 0.2, 1.0);
	}

	public static FAConnectedAnimationService GetForView(TopLevel topLevel)
	{
		if (topLevel == null)
		{
			return null;
		}
		FAConnectedAnimationService fAConnectedAnimationService = ((AvaloniaObject)topLevel).GetValue<FAConnectedAnimationService>((StyledProperty<FAConnectedAnimationService>)(object)ConnectedAnimationServiceProperty);
		if (fAConnectedAnimationService == null)
		{
			fAConnectedAnimationService = new FAConnectedAnimationService(topLevel);
			((AvaloniaObject)topLevel).SetValue<FAConnectedAnimationService>((StyledProperty<FAConnectedAnimationService>)(object)ConnectedAnimationServiceProperty, fAConnectedAnimationService, (BindingPriority)0);
		}
		return fAConnectedAnimationService;
	}

	public FAConnectedAnimation GetAnimation(string key)
	{
		if (_animations.TryGetValue(key, out var value))
		{
			_animations.Remove(key);
			return value;
		}
		return null;
	}

	public FAConnectedAnimation PrepareToAnimate(string key, Visual source)
	{
		if (string.IsNullOrEmpty(key))
		{
			throw new ArgumentException("Invalid key specified");
		}
		FAConnectedAnimation fAConnectedAnimation = new FAConnectedAnimation(source, this);
		if (_animations.ContainsKey(key))
		{
			_animations[key] = fAConnectedAnimation;
		}
		else
		{
			_animations.Add(key, fAConnectedAnimation);
		}
		return fAConnectedAnimation;
	}
}
