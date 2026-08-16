using System;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Media.Animation;

namespace FluentAvalonia.UI.Navigation;

/// <summary>
/// Provides data for navigation methods and event handlers that cannot cancel the navigation request.
/// </summary>
public class FANavigationEventArgs : RoutedEventArgs
{
	/// <summary>
	/// Gets the root node of the target page's content.
	/// </summary>
	public object Content { get; }

	/// <summary>
	/// Gets a value that indicates the direction of movement during navigation
	/// </summary>
	public FANavigationMode NavigationMode { get; }

	/// <summary>
	/// Gets any "Parameter" object passed to the target page for the navigation.
	/// </summary>
	public object Parameter { get; }

	/// <summary>
	/// Gets the data type of the source page.
	/// </summary>
	public Type SourcePageType { get; }

	/// <summary>
	/// Gets a value that indicates the animated transition associated with the navigation.
	/// </summary>
	public FANavigationTransitionInfo NavigationTransitionInfo { get; }

	internal FANavigationEventArgs(object content, FANavigationMode mode, FANavigationTransitionInfo navInfo, object param, Type srcPgType)
	{
		Content = content;
		NavigationMode = mode;
		NavigationTransitionInfo = navInfo;
		Parameter = param;
		SourcePageType = srcPgType;
	}
}
