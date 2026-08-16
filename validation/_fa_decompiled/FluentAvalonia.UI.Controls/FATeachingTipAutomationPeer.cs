using System.Runtime.CompilerServices;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// AutomationPeer for a <see cref="P:FluentAvalonia.UI.Controls.FATeachingTipAutomationPeer.TeachingTip" />
/// </summary>
public class FATeachingTipAutomationPeer : ContentControlAutomationPeer
{
	private FATeachingTip TeachingTip => Unsafe.As<FATeachingTip>(((ContentControlAutomationPeer)this).Owner);

	public bool Maximizable => false;

	public bool Minimizable => false;

	internal FATeachingTipAutomationPeer(FATeachingTip owner)
		: base((ContentControl)(object)owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		if (!TeachingTip.IsLightDismissEnabled)
		{
			return (AutomationControlType)35;
		}
		return (AutomationControlType)34;
	}

	protected override string GetClassNameCore()
	{
		return "TeachingTip";
	}

	public bool IsModal()
	{
		return TeachingTip.IsLightDismissEnabled;
	}

	public bool IsTopmost()
	{
		return TeachingTip.IsOpen;
	}

	public void Close()
	{
		TeachingTip.IsOpen = false;
	}

	public bool WaitForInputIdle(int milliseconds)
	{
		return true;
	}

	public void RaiseWindowClosedEvent()
	{
	}

	public void RaiseWindowOpenedEvent()
	{
	}
}
