using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Animation;

/// <summary>
/// Transitions between two pages by sliding them horizontally or vertically.
/// </summary>
public class PageSlide : IPageTransition, IProgressPageTransition
{
	/// <summary>
	/// The axis on which the PageSlide should occur
	/// </summary>
	public enum SlideAxis
	{
		Horizontal,
		Vertical
	}

	/// <summary>
	/// Gets the duration of the animation.
	/// </summary>
	public TimeSpan Duration { get; set; }

	/// <summary>
	/// Gets the orientation of the animation.
	/// </summary>
	public SlideAxis Orientation { get; set; }

	/// <summary>
	/// Gets or sets element entrance easing.
	/// </summary>
	public Easing SlideInEasing { get; set; } = new LinearEasing();

	/// <summary>
	/// Gets or sets element exit easing.
	/// </summary>
	public Easing SlideOutEasing { get; set; } = new LinearEasing();

	/// <summary>
	/// Gets or sets the fill mode applied to both slide animations.
	/// Defaults to <see cref="F:Avalonia.Animation.FillMode.Forward" />, which keeps the final transform value after
	/// the animation completes and prevents a one-frame flash where the outgoing element snaps
	/// back to its original position before <c>IsVisible = false</c> takes effect.
	/// </summary>
	public FillMode FillMode { get; set; } = FillMode.Forward;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Animation.PageSlide" /> class.
	/// </summary>
	public PageSlide()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Animation.PageSlide" /> class.
	/// </summary>
	/// <param name="duration">The duration of the animation.</param>
	/// <param name="orientation">The axis on which the animation should occur</param>
	public PageSlide(TimeSpan duration, SlideAxis orientation = SlideAxis.Horizontal)
	{
		Duration = duration;
		Orientation = orientation;
	}

	/// <inheritdoc />
	public virtual async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		List<Task> list = new List<Task>();
		Visual visualParent = GetVisualParent(from, to);
		double num = ((Orientation == SlideAxis.Horizontal) ? visualParent.Bounds.Width : visualParent.Bounds.Height);
		StyledProperty<double> property = ((Orientation == SlideAxis.Horizontal) ? TranslateTransform.XProperty : TranslateTransform.YProperty);
		if (from != null)
		{
			Animation animation = new Animation
			{
				FillMode = FillMode,
				Easing = SlideOutEasing,
				Children = 
				{
					new KeyFrame
					{
						Setters = { (IAnimationSetter)new Setter
						{
							Property = property,
							Value = 0.0
						} },
						Cue = new Cue(0.0)
					},
					new KeyFrame
					{
						Setters = { (IAnimationSetter)new Setter
						{
							Property = property,
							Value = (forward ? (0.0 - num) : num)
						} },
						Cue = new Cue(1.0)
					}
				},
				Duration = Duration
			};
			list.Add(animation.RunAsync(from, null, cancellationToken));
		}
		if (to != null)
		{
			to.IsVisible = true;
			Animation animation2 = new Animation
			{
				FillMode = FillMode,
				Easing = SlideInEasing,
				Children = 
				{
					new KeyFrame
					{
						Setters = { (IAnimationSetter)new Setter
						{
							Property = property,
							Value = (forward ? num : (0.0 - num))
						} },
						Cue = new Cue(0.0)
					},
					new KeyFrame
					{
						Setters = { (IAnimationSetter)new Setter
						{
							Property = property,
							Value = 0.0
						} },
						Cue = new Cue(1.0)
					}
				},
				Duration = Duration
			};
			list.Add(animation2.RunAsync(to, null, cancellationToken));
		}
		await Task.WhenAll(list);
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		if (from != null)
		{
			from.IsVisible = false;
			if (FillMode != FillMode.None)
			{
				from.RenderTransform = null;
			}
		}
		if (to != null && FillMode != FillMode.None)
		{
			to.RenderTransform = null;
		}
	}

	/// <inheritdoc />
	public virtual void Update(double progress, Visual? from, Visual? to, bool forward, double pageLength, IReadOnlyList<PageTransitionItem> visibleItems)
	{
		if (visibleItems.Count > 0 || (from == null && to == null))
		{
			return;
		}
		Visual visualParent = GetVisualParent(from, to);
		double num = ((pageLength > 0.0) ? pageLength : ((Orientation == SlideAxis.Horizontal) ? visualParent.Bounds.Width : visualParent.Bounds.Height));
		double num2 = num * progress;
		if (from != null)
		{
			TranslateTransform translateTransform = from.RenderTransform as TranslateTransform;
			if (translateTransform == null)
			{
				translateTransform = (TranslateTransform)(from.RenderTransform = new TranslateTransform());
			}
			if (Orientation == SlideAxis.Horizontal)
			{
				translateTransform.X = (forward ? (0.0 - num2) : num2);
			}
			else
			{
				translateTransform.Y = (forward ? (0.0 - num2) : num2);
			}
		}
		if (to != null)
		{
			to.IsVisible = true;
			TranslateTransform translateTransform2 = to.RenderTransform as TranslateTransform;
			if (translateTransform2 == null)
			{
				translateTransform2 = (TranslateTransform)(to.RenderTransform = new TranslateTransform());
			}
			if (Orientation == SlideAxis.Horizontal)
			{
				translateTransform2.X = (forward ? (num - num2) : (0.0 - (num - num2)));
			}
			else
			{
				translateTransform2.Y = (forward ? (num - num2) : (0.0 - (num - num2)));
			}
		}
	}

	/// <inheritdoc />
	public virtual void Reset(Visual visual)
	{
		visual.RenderTransform = null;
	}

	/// <summary>
	/// Gets the common visual parent of the two control.
	/// </summary>
	/// <param name="from">The from control.</param>
	/// <param name="to">The to control.</param>
	/// <returns>The common parent.</returns>
	/// <exception cref="T:System.ArgumentException">
	/// The two controls do not share a common parent.
	/// </exception>
	/// <remarks>
	/// Any one of the parameters may be null, but not both.
	/// </remarks>
	protected static Visual GetVisualParent(Visual? from, Visual? to)
	{
		Visual visualParent = (from ?? to).VisualParent;
		Visual visualParent2 = (to ?? from).VisualParent;
		if (visualParent != null && visualParent2 != null && visualParent != visualParent2)
		{
			throw new ArgumentException("Controls for PageSlide must have same parent.");
		}
		return visualParent ?? throw new InvalidOperationException("Cannot determine visual parent.");
	}
}
