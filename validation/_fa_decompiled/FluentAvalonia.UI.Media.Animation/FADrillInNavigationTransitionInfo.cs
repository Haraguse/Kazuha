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
/// Specifies the animation to run when a user navigates forward in a logical hierarchy, 
/// like from a master list to a detail page.
/// </summary>
public class FADrillInNavigationTransitionInfo : FANavigationTransitionInfo
{
	/// <summary>
	/// Gets or sets whether the animation should drill in (false) or drill out (true)
	/// </summary>
	public bool IsReversed { get; set; }

	public override async void RunAnimation(Animatable ctrl, CancellationToken cancellationToken)
	{
		Animation val = new Animation
		{
			Easing = (Easing)new SplineEasing(0.1, 0.9, 0.2, 1.0)
		};
		KeyFrames children = val.Children;
		KeyFrame val2 = new KeyFrame();
		val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.OpacityProperty, (object)0.0));
		val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)ScaleTransform.ScaleXProperty, (object)(IsReversed ? 1.5 : 0.0)));
		val2.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)ScaleTransform.ScaleYProperty, (object)(IsReversed ? 1.5 : 0.0)));
		val2.Cue = new Cue(0.0);
		((AvaloniaList<KeyFrame>)(object)children).Add(val2);
		KeyFrames children2 = val.Children;
		KeyFrame val3 = new KeyFrame();
		val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)Visual.OpacityProperty, (object)1.0));
		val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)ScaleTransform.ScaleXProperty, (object)(IsReversed ? 1.0 : 1.0)));
		val3.Setters.Add((IAnimationSetter)new Setter((AvaloniaProperty)(object)ScaleTransform.ScaleYProperty, (object)(IsReversed ? 1.0 : 1.0)));
		val3.Cue = new Cue(1.0);
		((AvaloniaList<KeyFrame>)(object)children2).Add(val3);
		val.Duration = TimeSpan.FromSeconds(0.67);
		val.FillMode = (FillMode)1;
		await val.RunAsync(ctrl, cancellationToken);
		((Visual)((ctrl is Visual) ? ctrl : null)).Opacity = 1.0;
	}
}
