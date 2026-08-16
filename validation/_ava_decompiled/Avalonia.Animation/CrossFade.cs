using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Styling;

namespace Avalonia.Animation;

/// <summary>
/// Defines a cross-fade animation between two <see cref="T:Avalonia.Visual" />s.
/// </summary>
public class CrossFade : IPageTransition, IProgressPageTransition
{
	private const double SidePeekOpacity = 0.72;

	private const double FarPeekOpacity = 0.42;

	private const double OutgoingDip = 0.22;

	private const double IncomingBoost = 0.12;

	private const double PassiveDip = 0.05;

	private readonly Animation _fadeOutAnimation;

	private readonly Animation _fadeInAnimation;

	/// <summary>
	/// Gets the duration of the animation.
	/// </summary>
	public TimeSpan Duration
	{
		get
		{
			return _fadeOutAnimation.Duration;
		}
		set
		{
			Animation fadeOutAnimation = _fadeOutAnimation;
			TimeSpan duration = (_fadeInAnimation.Duration = value);
			fadeOutAnimation.Duration = duration;
		}
	}

	/// <summary>
	/// Gets or sets element entrance easing.
	/// </summary>
	public Easing FadeInEasing
	{
		get
		{
			return _fadeInAnimation.Easing;
		}
		set
		{
			_fadeInAnimation.Easing = value;
		}
	}

	/// <summary>
	/// Gets or sets element exit easing.
	/// </summary>
	public Easing FadeOutEasing
	{
		get
		{
			return _fadeOutAnimation.Easing;
		}
		set
		{
			_fadeOutAnimation.Easing = value;
		}
	}

	/// <summary>
	/// Gets or sets the fill mode applied to both fade animations.
	/// Defaults to <see cref="F:Avalonia.Animation.FillMode.Forward" />.
	/// </summary>
	public FillMode FillMode
	{
		get
		{
			return _fadeOutAnimation.FillMode;
		}
		set
		{
			Animation fadeOutAnimation = _fadeOutAnimation;
			FillMode fillMode = (_fadeInAnimation.FillMode = value);
			fadeOutAnimation.FillMode = fillMode;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Animation.CrossFade" /> class.
	/// </summary>
	public CrossFade()
		: this(TimeSpan.Zero)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Animation.CrossFade" /> class.
	/// </summary>
	/// <param name="duration">The duration of the animation.</param>
	public CrossFade(TimeSpan duration)
	{
		_fadeOutAnimation = new Animation
		{
			FillMode = FillMode.Forward,
			Children = 
			{
				new KeyFrame
				{
					Setters = { (IAnimationSetter)new Setter
					{
						Property = Visual.OpacityProperty,
						Value = 1.0
					} },
					Cue = new Cue(0.0)
				},
				new KeyFrame
				{
					Setters = { (IAnimationSetter)new Setter
					{
						Property = Visual.OpacityProperty,
						Value = 0.0
					} },
					Cue = new Cue(1.0)
				}
			}
		};
		_fadeInAnimation = new Animation
		{
			FillMode = FillMode.Forward,
			Children = 
			{
				new KeyFrame
				{
					Setters = { (IAnimationSetter)new Setter
					{
						Property = Visual.OpacityProperty,
						Value = 0.0
					} },
					Cue = new Cue(0.0)
				},
				new KeyFrame
				{
					Setters = { (IAnimationSetter)new Setter
					{
						Property = Visual.OpacityProperty,
						Value = 1.0
					} },
					Cue = new Cue(1.0)
				}
			}
		};
		_fadeOutAnimation.Duration = (_fadeInAnimation.Duration = duration);
	}

	/// <inheritdoc cref="M:Avalonia.Animation.CrossFade.Start(Avalonia.Visual,Avalonia.Visual,System.Threading.CancellationToken)" />
	public async Task Start(Visual? from, Visual? to, CancellationToken cancellationToken)
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			List<Task> list = new List<Task>();
			if (from != null)
			{
				list.Add(_fadeOutAnimation.RunAsync(from, null, cancellationToken));
			}
			if (to != null)
			{
				to.IsVisible = true;
				list.Add(_fadeInAnimation.RunAsync(to, null, cancellationToken));
			}
			await Task.WhenAll(list);
			if (from != null && !cancellationToken.IsCancellationRequested)
			{
				from.IsVisible = false;
			}
		}
	}

	/// <summary>
	/// Starts the animation.
	/// </summary>
	/// <param name="from">
	/// The control that is being transitioned away from. May be null.
	/// </param>
	/// <param name="to">
	/// The control that is being transitioned to. May be null.
	/// </param>
	/// <param name="forward">
	/// Unused for cross-fades.
	/// </param>
	/// <param name="cancellationToken">allowed cancel transition</param>
	/// <returns>
	/// A <see cref="T:System.Threading.Tasks.Task" /> that tracks the progress of the animation.
	/// </returns>
	Task IPageTransition.Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
	{
		return Start(from, to, cancellationToken);
	}

	/// <inheritdoc />
	public void Update(double progress, Visual? from, Visual? to, bool forward, double pageLength, IReadOnlyList<PageTransitionItem> visibleItems)
	{
		if (visibleItems.Count > 0)
		{
			UpdateVisibleItems(progress, from, to, visibleItems);
			return;
		}
		if (from != null)
		{
			from.Opacity = 1.0 - progress;
		}
		if (to != null)
		{
			to.IsVisible = true;
			to.Opacity = progress;
		}
	}

	/// <inheritdoc />
	public void Reset(Visual visual)
	{
		visual.Opacity = 1.0;
	}

	private static void UpdateVisibleItems(double progress, Visual? from, Visual? to, IReadOnlyList<PageTransitionItem> visibleItems)
	{
		double num = Math.Sin(Math.Clamp(progress, 0.0, 1.0) * Math.PI);
		using IEnumerator<PageTransitionItem> enumerator = visibleItems.GetEnumerator();
		while (enumerator.MoveNext())
		{
			PageTransitionItem pageTransitionItem = enumerator.Current with
			{
				Visual = 
				{
					IsVisible = true
				}
			};
			double opacityForOffset = GetOpacityForOffset(pageTransitionItem.ViewportCenterOffset);
			opacityForOffset = ((pageTransitionItem.Visual != from) ? ((pageTransitionItem.Visual != to) ? Math.Max(0.42, opacityForOffset - 0.05 * num) : Math.Min(1.0, opacityForOffset + 0.12 * num)) : Math.Max(0.42, opacityForOffset - 0.22 * num));
			pageTransitionItem.Visual.Opacity = opacityForOffset;
		}
	}

	private static double GetOpacityForOffset(double offsetFromCenter)
	{
		double num = Math.Abs(offsetFromCenter);
		if (num <= 1.0)
		{
			return Lerp(1.0, 0.72, num);
		}
		if (num <= 2.0)
		{
			return Lerp(0.72, 0.42, num - 1.0);
		}
		return 0.42;
	}

	private static double Lerp(double from, double to, double t)
	{
		return from + (to - from) * Math.Clamp(t, 0.0, 1.0);
	}
}
