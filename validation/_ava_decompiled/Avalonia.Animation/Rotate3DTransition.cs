using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Animation;

public class Rotate3DTransition : PageSlide
{
	private const double SidePeekAngle = 24.0;

	private const double FarPeekAngle = 38.0;

	/// <summary>
	///  Defines the depth of the 3D Effect. If null, depth will be calculated automatically from the width or height
	///  of the common parent of the visual being rotated.
	/// </summary>
	public double? Depth { get; set; }

	/// <summary>
	///  Creates a new instance of the <see cref="T:Avalonia.Animation.Rotate3DTransition" />
	/// </summary>
	/// <param name="duration">How long the rotation should take place</param>
	/// <param name="orientation">The orientation of the rotation</param>
	/// <param name="depth">Defines the depth of the 3D Effect. If null, depth will be calculated automatically from the width or height of the common parent of the visual being rotated</param>
	public Rotate3DTransition(TimeSpan duration, SlideAxis orientation = SlideAxis.Horizontal, double? depth = null)
		: base(duration, orientation)
	{
		Depth = depth;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Animation.Rotate3DTransition" /> class.
	/// </summary>
	public Rotate3DTransition()
	{
	}

	/// <inheritdoc />
	public override async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		Task[] array = new Task[(from == null || to == null) ? 1 : 2];
		Visual visualParent = PageSlide.GetVisualParent(from, to);
		(StyledProperty<double>, double) tuple = base.Orientation switch
		{
			SlideAxis.Vertical => (Rotate3DTransform.AngleXProperty, visualParent.Bounds.Height), 
			SlideAxis.Horizontal => (Rotate3DTransform.AngleYProperty, visualParent.Bounds.Width), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		StyledProperty<double> rotateProperty = tuple.Item1;
		double item = tuple.Item2;
		Setter depthSetter = new Setter
		{
			Property = Rotate3DTransform.DepthProperty,
			Value = (Depth ?? item)
		};
		Setter centerZSetter = new Setter
		{
			Property = Rotate3DTransform.CenterZProperty,
			Value = (0.0 - item) / 2.0
		};
		if (from != null)
		{
			Animation animation = new Animation
			{
				Easing = base.SlideOutEasing,
				Duration = base.Duration,
				FillMode = base.FillMode,
				Children = 
				{
					CreateKeyFrame(0.0, 0.0, 2),
					CreateKeyFrame(0.5, 45.0 * (double)((!forward) ? 1 : (-1)), 1),
					CreateKeyFrame(1.0, 90.0 * (double)((!forward) ? 1 : (-1)), 1, isVisible: false)
				}
			};
			array[0] = animation.RunAsync(from, null, cancellationToken);
		}
		if (to != null)
		{
			to.IsVisible = true;
			Animation animation2 = new Animation
			{
				Easing = base.SlideInEasing,
				Duration = base.Duration,
				FillMode = base.FillMode,
				Children = 
				{
					CreateKeyFrame(0.0, 90.0 * (double)(forward ? 1 : (-1)), 1),
					CreateKeyFrame(0.5, 45.0 * (double)(forward ? 1 : (-1)), 1),
					CreateKeyFrame(1.0, 0.0, 2)
				}
			};
			array[(from != null) ? 1u : 0u] = animation2.RunAsync(to, null, cancellationToken);
		}
		await Task.WhenAll(array);
		if (!cancellationToken.IsCancellationRequested)
		{
			if (to != null)
			{
				to.ZIndex = 2;
			}
			if (from != null)
			{
				from.IsVisible = false;
				from.ZIndex = 1;
			}
		}
		KeyFrame CreateKeyFrame(double cue, double rotation, int zIndex, bool isVisible = true)
		{
			return new KeyFrame
			{
				Setters = 
				{
					(IAnimationSetter)new Setter
					{
						Property = rotateProperty,
						Value = rotation
					},
					(IAnimationSetter)new Setter
					{
						Property = Visual.ZIndexProperty,
						Value = zIndex
					},
					(IAnimationSetter)new Setter
					{
						Property = Visual.IsVisibleProperty,
						Value = isVisible
					},
					(IAnimationSetter)centerZSetter,
					(IAnimationSetter)depthSetter
				},
				Cue = new Cue(cue)
			};
		}
	}

	/// <inheritdoc />
	public override void Update(double progress, Visual? from, Visual? to, bool forward, double pageLength, IReadOnlyList<PageTransitionItem> visibleItems)
	{
		if (visibleItems.Count > 0)
		{
			UpdateVisibleItems(progress, from, to, pageLength, visibleItems);
		}
		else
		{
			if (from == null && to == null)
			{
				return;
			}
			Visual visualParent = PageSlide.GetVisualParent(from, to);
			double num = ((pageLength > 0.0) ? pageLength : ((base.Orientation == SlideAxis.Vertical) ? visualParent.Bounds.Height : visualParent.Bounds.Width));
			double depth = Depth ?? num;
			double num2 = (forward ? 1.0 : (-1.0));
			if (from != null)
			{
				Rotate3DTransform rotate3DTransform = from.RenderTransform as Rotate3DTransform;
				if (rotate3DTransform == null)
				{
					rotate3DTransform = (Rotate3DTransform)(from.RenderTransform = new Rotate3DTransform());
				}
				rotate3DTransform.Depth = depth;
				rotate3DTransform.CenterZ = (0.0 - num) / 2.0;
				from.ZIndex = ((!(progress < 0.5)) ? 1 : 2);
				if (base.Orientation == SlideAxis.Horizontal)
				{
					rotate3DTransform.AngleY = (0.0 - num2) * 90.0 * progress;
				}
				else
				{
					rotate3DTransform.AngleX = (0.0 - num2) * 90.0 * progress;
				}
			}
			if (to != null)
			{
				to.IsVisible = true;
				Rotate3DTransform rotate3DTransform2 = to.RenderTransform as Rotate3DTransform;
				if (rotate3DTransform2 == null)
				{
					rotate3DTransform2 = (Rotate3DTransform)(to.RenderTransform = new Rotate3DTransform());
				}
				rotate3DTransform2.Depth = depth;
				rotate3DTransform2.CenterZ = (0.0 - num) / 2.0;
				to.ZIndex = ((progress < 0.5) ? 1 : 2);
				if (base.Orientation == SlideAxis.Horizontal)
				{
					rotate3DTransform2.AngleY = num2 * 90.0 * (1.0 - progress);
				}
				else
				{
					rotate3DTransform2.AngleX = num2 * 90.0 * (1.0 - progress);
				}
			}
		}
	}

	private void UpdateVisibleItems(double progress, Visual? from, Visual? to, double pageLength, IReadOnlyList<PageTransitionItem> visibleItems)
	{
		Visual visualParent = (from ?? to ?? visibleItems[0].Visual).VisualParent;
		if (visualParent == null)
		{
			return;
		}
		double num = ((pageLength > 0.0) ? pageLength : ((base.Orientation == SlideAxis.Vertical) ? visualParent.Bounds.Height : visualParent.Bounds.Width));
		double depth = Depth ?? num;
		double num2 = Math.Sin(Math.Clamp(progress, 0.0, 1.0) * Math.PI);
		foreach (PageTransitionItem visibleItem in visibleItems)
		{
			Visual visual = visibleItem.Visual;
			visual.IsVisible = true;
			visual.ZIndex = GetZIndex(visibleItem.ViewportCenterOffset);
			Rotate3DTransform rotate3DTransform = visual.RenderTransform as Rotate3DTransform;
			if (rotate3DTransform == null)
			{
				rotate3DTransform = (Rotate3DTransform)(visual.RenderTransform = new Rotate3DTransform());
			}
			rotate3DTransform.Depth = depth;
			rotate3DTransform.CenterZ = (0.0 - num) / 2.0;
			double num3 = GetAngleForOffset(visibleItem.ViewportCenterOffset) * num2;
			if (base.Orientation == SlideAxis.Horizontal)
			{
				rotate3DTransform.AngleY = num3;
				rotate3DTransform.AngleX = 0.0;
			}
			else
			{
				rotate3DTransform.AngleX = num3;
				rotate3DTransform.AngleY = 0.0;
			}
		}
	}

	private static double GetAngleForOffset(double offsetFromCenter)
	{
		int num = Math.Sign(offsetFromCenter);
		if (num == 0)
		{
			return 0.0;
		}
		double num2 = Math.Abs(offsetFromCenter);
		if (num2 <= 1.0)
		{
			return (double)num * Lerp(0.0, 24.0, num2);
		}
		if (num2 <= 2.0)
		{
			return (double)num * Lerp(24.0, 38.0, num2 - 1.0);
		}
		return (double)num * 38.0;
	}

	private static int GetZIndex(double offsetFromCenter)
	{
		double num = Math.Abs(offsetFromCenter);
		if (num < 0.5)
		{
			return 3;
		}
		if (num < 1.5)
		{
			return 2;
		}
		return 1;
	}

	private static double Lerp(double from, double to, double t)
	{
		return from + (to - from) * Math.Clamp(t, 0.0, 1.0);
	}

	/// <inheritdoc />
	public override void Reset(Visual visual)
	{
		visual.RenderTransform = null;
		visual.ZIndex = 0;
	}
}
