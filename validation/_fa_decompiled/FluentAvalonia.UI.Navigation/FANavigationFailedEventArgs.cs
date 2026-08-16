using System;

namespace FluentAvalonia.UI.Navigation;

/// <summary>
/// Provides event data for the Frame.NavigationFailed event.
/// </summary>
public class FANavigationFailedEventArgs : EventArgs
{
	/// <summary>
	/// Gets or sets a value that indicates whether the failure event has been handled.
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Gets the result code for the exception that is associated with the failed navigation.
	/// </summary>
	public Exception Exception { get; }

	/// <summary>
	/// Gets the data type of the target page.
	/// </summary>
	public Type SourcePageType { get; }

	internal FANavigationFailedEventArgs(Exception ex, Type srcPageType)
	{
		Exception = ex;
		SourcePageType = srcPageType;
	}
}
