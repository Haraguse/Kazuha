namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Defines constants that indicate the preferred location of the <see cref="P:FluentAvalonia.UI.Controls.FATeachingTip.HeroContent" /> 
/// within a <see cref="T:FluentAvalonia.UI.Controls.FATeachingTip" />.
/// </summary>
public enum FATeachingTipHeroContentPlacementMode
{
	/// <summary>
	/// The header of the teaching tip. The hero content might be moved to the footer to avoid 
	/// intersecting with the tail of the targeted teaching tip.
	/// </summary>
	Auto,
	/// <summary>
	/// The header of the teaching tip.
	/// </summary>
	Top,
	/// <summary>
	/// The footer of the teaching tip.
	/// </summary>
	Bottom
}
