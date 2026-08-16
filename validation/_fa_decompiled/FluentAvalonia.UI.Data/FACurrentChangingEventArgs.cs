using System;
using System.Runtime.InteropServices;

namespace FluentAvalonia.UI.Data;

/// <summary>
/// Provides data for the CurrentChanging event
/// </summary>
public class FACurrentChangingEventArgs : EventArgs
{
	/// <summary>
	/// Gets or sets a value that indicates whether the CurrentItem change should be cancelled
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Gets a value that indicates whether the CurrentItem change can be canceled
	/// </summary>
	public bool IsCancelable { get; }

	/// <summary>
	/// Initializes a new instance of the CurrentChangingEventArgs class
	/// </summary>
	public FACurrentChangingEventArgs()
	{
	}

	/// <summary>
	/// Initializes a new instance of the CurrentChangingEventArgs class
	/// </summary>
	/// <param name="canCancel">True to disable the ability to cancel a CurrentItem change; False 
	/// to enable cancellation</param>
	public FACurrentChangingEventArgs([In] bool canCancel)
	{
		IsCancelable = canCancel;
	}
}
