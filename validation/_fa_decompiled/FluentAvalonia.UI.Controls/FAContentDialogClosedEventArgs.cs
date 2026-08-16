using System;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides data for the Closed event.
/// </summary>
public class FAContentDialogClosedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the <see cref="T:FluentAvalonia.UI.Controls.FAContentDialogResult" /> of the button click event.
	/// </summary>
	public FAContentDialogResult Result { get; }

	internal FAContentDialogClosedEventArgs(FAContentDialogResult res)
	{
		Result = res;
	}
}
