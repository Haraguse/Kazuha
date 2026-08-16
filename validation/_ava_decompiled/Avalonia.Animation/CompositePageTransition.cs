using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Animation;

/// <summary>
/// Defines a composite page transition that can be used to combine multiple transitions.
/// </summary>
/// <remarks>
/// <para>
/// Instantiate the <see cref="T:Avalonia.Animation.CompositePageTransition" /> in XAML and initialize the
/// <see cref="T:Avalonia.Animation.Transitions" /> property in order to have many animations triggered at once.
/// For example, you can combine <see cref="T:Avalonia.Animation.CrossFade" /> and <see cref="T:Avalonia.Animation.PageSlide" />.
/// <code>
/// <![CDATA[
/// <reactiveUi:RoutedViewHost Router="{Binding Router}">
///   <reactiveUi:RoutedViewHost.PageTransition>
///     <CompositePageTransition>
///       <PageSlide Duration="0.5" />
///       <CrossFade Duration="0.5" />
///     </CompositePageTransition>
///   </reactiveUi:RoutedViewHost.PageTransition>
/// </reactiveUi:RoutedViewHost>
/// ]]>
/// </code>
/// </para>
/// </remarks>
public class CompositePageTransition : IPageTransition, IProgressPageTransition
{
	/// <summary>
	/// Gets or sets the transitions to be executed. Can be defined from XAML.
	/// </summary>
	[Content]
	public List<IPageTransition> PageTransitions { get; set; } = new List<IPageTransition>();

	/// <inheritdoc />
	public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
	{
		return Task.WhenAll(PageTransitions.Select((IPageTransition transition) => transition.Start(from, to, forward, cancellationToken)).ToArray());
	}

	/// <inheritdoc />
	public void Update(double progress, Visual? from, Visual? to, bool forward, double pageLength, IReadOnlyList<PageTransitionItem> visibleItems)
	{
		foreach (IPageTransition pageTransition in PageTransitions)
		{
			if (pageTransition is IProgressPageTransition progressPageTransition)
			{
				progressPageTransition.Update(progress, from, to, forward, pageLength, visibleItems);
			}
		}
	}

	/// <inheritdoc />
	public void Reset(Visual visual)
	{
		foreach (IPageTransition pageTransition in PageTransitions)
		{
			if (pageTransition is IProgressPageTransition progressPageTransition)
			{
				progressPageTransition.Reset(visual);
			}
		}
	}
}
