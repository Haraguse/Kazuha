using System;
using System.Text;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Animation;

/// <summary>
/// Stores data regarding a specific key
/// point and value in an animation.
/// </summary>
public sealed class KeyFrame : AvaloniaObject
{
	private TimeSpan _ktimeSpan;

	private Cue _kCue;

	private KeySpline? _kKeySpline;

	/// <summary>
	/// Gets the setters of <see cref="T:Avalonia.Animation.KeyFrame" />.
	/// </summary>
	[Content]
	public AvaloniaList<IAnimationSetter> Setters { get; } = new AvaloniaList<IAnimationSetter>();

	internal KeyFrameTimingMode TimingMode { get; private set; }

	/// <summary>
	/// Gets or sets the key time of this <see cref="T:Avalonia.Animation.KeyFrame" />.
	/// </summary>
	/// <value>The key time.</value>
	public TimeSpan KeyTime
	{
		get
		{
			return _ktimeSpan;
		}
		set
		{
			if (TimingMode == KeyFrameTimingMode.Cue)
			{
				throw new InvalidOperationException("You can only set either KeyTime or Cue.");
			}
			TimingMode = KeyFrameTimingMode.TimeSpan;
			_ktimeSpan = value;
		}
	}

	/// <summary>
	/// Gets or sets the cue of this <see cref="T:Avalonia.Animation.KeyFrame" />.
	/// </summary>
	/// <value>The cue.</value>
	public Cue Cue
	{
		get
		{
			return _kCue;
		}
		set
		{
			if (TimingMode == KeyFrameTimingMode.TimeSpan)
			{
				throw new InvalidOperationException("You can only set either KeyTime or Cue.");
			}
			TimingMode = KeyFrameTimingMode.Cue;
			_kCue = value;
		}
	}

	/// <summary>
	/// Gets or sets the KeySpline of this <see cref="T:Avalonia.Animation.KeyFrame" />.
	/// </summary>
	/// <value>The key spline.</value>
	public KeySpline? KeySpline
	{
		get
		{
			return _kKeySpline;
		}
		set
		{
			_kKeySpline = value;
			if (value != null && !value.IsValid())
			{
				throw new ArgumentException("KeySpline must have X coordinates >= 0.0 and <= 1.0.");
			}
		}
	}

	internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent)
	{
		base.BuildDebugDisplay(builder, includeContent);
		switch (TimingMode)
		{
		case KeyFrameTimingMode.TimeSpan:
			DebugDisplayHelper.AppendOptionalValue(builder, "KeyTime", KeyTime, includeContent);
			break;
		case KeyFrameTimingMode.Cue:
			DebugDisplayHelper.AppendOptionalValue(builder, "Cue", Cue, includeContent);
			break;
		}
	}
}
