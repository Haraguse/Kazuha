using System.Threading;
using Avalonia;
using Avalonia.Animation;

namespace FluentAvalonia.UI.Media.Animation;

/// <summary>
/// Specifies that animations are suppressed during navigation.
/// </summary>
public class FASuppressNavigationTransitionInfo : FANavigationTransitionInfo
{
	public override void RunAnimation(Animatable ctrl, CancellationToken cancellationToken)
	{
		((Visual)((ctrl is Visual) ? ctrl : null)).Opacity = 1.0;
	}
}
