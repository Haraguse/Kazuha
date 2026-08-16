using System;
using Avalonia.Threading;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides data for the closing event.
/// </summary>
public class FATaskDialogClosingEventArgs : EventArgs
{
	private FADeferral _deferral;

	private int _deferralCount;

	/// <summary>
	/// Gets or sets a value that can cancel the closing of the dialog.
	/// A true value for Cancel cancels the default behavior.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Gets the result of the closing event.
	/// </summary>
	public object Result { get; }

	internal FATaskDialogClosingEventArgs(object res)
	{
		Result = res;
	}

	/// <summary>
	/// Gets a <see cref="T:FluentAvalonia.Core.FADeferral" /> that the app can use to 
	/// respond asynchronously to the closing event.
	/// </summary>
	/// <returns></returns>
	public FADeferral GetDeferral()
	{
		_deferralCount++;
		return new FADeferral(delegate
		{
			Dispatcher.UIThread.VerifyAccess();
			DecrementDeferralCount();
		});
	}

	internal void SetDeferral(FADeferral deferral)
	{
		_deferral = deferral;
	}

	internal void IncrementDeferralCount()
	{
		_deferralCount++;
	}

	internal void DecrementDeferralCount()
	{
		_deferralCount--;
		if (_deferralCount == 0)
		{
			_deferral.Complete();
		}
	}
}
