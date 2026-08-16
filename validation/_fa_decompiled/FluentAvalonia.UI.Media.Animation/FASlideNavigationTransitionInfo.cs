using System;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Styling;

namespace FluentAvalonia.UI.Media.Animation;

/// <summary>
/// Provides the parameters for a slide navigation transition.
/// </summary>
public class FASlideNavigationTransitionInfo : FANavigationTransitionInfo
{
	/// <summary>
	/// Gets or sets the type of animation effect to play during the slide transition.
	/// </summary>
	public FASlideNavigationTransitionEffect Effect { get; set; } = FASlideNavigationTransitionEffect.FromRight;

	/// <summary>
	/// Gets or sets the HorizontalOffset used when animating from the Left or Right
	/// </summary>
	public double FromHorizontalOffset { get; set; } = 56.0;

	/// <summary>
	/// Gets or sets the VerticalOffset used when animating from the Top or Bottom
	/// </summary>
	public double FromVerticalOffset { get; set; } = 56.0;

	public override async void RunAnimation(Animatable ctrl, CancellationToken cancellationToken)
	{
		double num = 0.0;
		bool flag = false;
		switch (Effect)
		{
		case FASlideNavigationTransitionEffect.FromLeft:
			num = 0.0 - FromHorizontalOffset;
			break;
		case FASlideNavigationTransitionEffect.FromRight:
			num = FromHorizontalOffset;
			break;
		case FASlideNavigationTransitionEffect.FromTop:
			num = 0.0 - FromVerticalOffset;
			flag = true;
			break;
		case FASlideNavigationTransitionEffect.FromBottom:
			num = FromVerticalOffset;
			flag = true;
			break;
		}
		Animation val = new Animation
		{
			Easing = (Easing)new SplineEasing(0.1, 0.9, 0.2, 1.0)
		};
		KeyFrames children = val.Children;
		KeyFrame val2 = new KeyFrame();
		val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)(flag ? TranslateTransform.YProperty : TranslateTransform.XProperty), (object)num));
		val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.OpacityProperty, (object)0.0));
		val2.Cue = new Cue(0.0);
		((AvaloniaList<KeyFrame>)(object)children).Add(val2);
		KeyFrames children2 = val.Children;
		KeyFrame val3 = new KeyFrame();
		val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.OpacityProperty, (object)1.0));
		val3.Cue = new Cue(0.05);
		((AvaloniaList<KeyFrame>)(object)children2).Add(val3);
		KeyFrames children3 = val.Children;
		KeyFrame val4 = new KeyFrame();
		val4.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.OpacityProperty, (object)1.0));
		val4.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)(flag ? TranslateTransform.YProperty : TranslateTransform.XProperty), (object)0.0));
		val4.Cue = new Cue(1.0);
		((AvaloniaList<KeyFrame>)(object)children3).Add(val4);
		val.Duration = TimeSpan.FromSeconds(0.167);
		val.FillMode = (FillMode)1;
		await val.RunAsync(ctrl, cancellationToken);
		((Visual)((ctrl is Visual) ? ctrl : null)).Opacity = 1.0;
	}
}
