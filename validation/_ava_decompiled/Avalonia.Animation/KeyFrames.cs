using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Animation;

/// <summary>
/// A collection of <see cref="T:Avalonia.Animation.KeyFrame" />s.
/// </summary>
public sealed class KeyFrames : AvaloniaList<KeyFrame>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Animation.KeyFrames" /> class.
	/// </summary>
	public KeyFrames()
	{
		base.ResetBehavior = ResetBehavior.Remove;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Avalonia.Animation.KeyFrames" /> class.
	/// </summary>
	/// <param name="items">The initial items in the collection.</param>
	public KeyFrames(IEnumerable<KeyFrame> items)
		: base(items)
	{
		base.ResetBehavior = ResetBehavior.Remove;
	}
}
