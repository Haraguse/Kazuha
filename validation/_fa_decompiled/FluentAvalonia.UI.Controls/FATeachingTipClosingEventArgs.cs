using System.ComponentModel;
using Avalonia.Threading;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides data for the <see cref="E:FluentAvalonia.UI.Controls.FATeachingTip.Closing" /> event.
/// </summary>
public class FATeachingTipClosingEventArgs : CancelEventArgs
{
	private int _deferralCount;

	private FADeferral _deferral;

	/// <summary>
	/// Gets a constant that specifies whether the cause of the Closing event was due to 
	/// user interaction (Close button click), light-dismissal, or programmatic closure.
	/// </summary>
	public FATeachingTipCloseReason Reason { get; }

	internal FATeachingTipClosingEventArgs(FATeachingTipCloseReason reason)
	{
		Reason = reason;
	}

	/// <summary>
	/// Gets a <see cref="T:FluentAvalonia.Core.FADeferral" /> object for managing the work done in the Closing event handler.
	/// </summary>
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

	internal void DecrementDeferralCount()
	{
		_deferralCount--;
		if (_deferralCount == 0)
		{
			_deferral.Complete();
		}
	}

	internal void IncrementDeferralCount()
	{
		_deferralCount++;
	}
}
