using System;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Media.Animation;

namespace FluentAvalonia.UI.Navigation;

/// <summary>
/// Provides data for the OnNavigatingFrom callback that can be used to cancel a navigation request from origination.
/// </summary>
public class FANavigatingCancelEventArgs : RoutedEventArgs
{
	/// <summary>
	/// Specifies whether a pending navigation should be canceled.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Gets the value of the mode parameter from the originating Navigate call.
	/// </summary>
	public FANavigationMode NavigationMode { get; }

	/// <summary>
	/// Gets the value of the SourcePageType parameter from the originating Navigate call.
	/// </summary>
	public Type SourcePageType { get; }

	/// <summary>
	/// Gets a value that indicates the animated transition associated with the navigation.
	/// </summary>
	public FANavigationTransitionInfo NavigationTransitionInfo { get; }

	/// <summary>
	/// Gets the navigation parameter associated with this navigation.
	/// </summary>
	public object Parameter { get; }

	internal FANavigatingCancelEventArgs(FANavigationMode mode, FANavigationTransitionInfo info, object param, Type srcType)
	{
		NavigationMode = mode;
		NavigationTransitionInfo = info;
		Parameter = param;
		SourcePageType = srcType;
	}
}
