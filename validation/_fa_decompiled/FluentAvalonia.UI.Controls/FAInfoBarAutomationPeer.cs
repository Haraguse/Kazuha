using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the AutomationPeer for a <see cref="T:FluentAvalonia.UI.Controls.FAInfoBar" />
/// </summary>
public sealed class FAInfoBarAutomationPeer : ControlAutomationPeer
{
	public FAInfoBarAutomationPeer(Control owner)
		: base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return (AutomationControlType)19;
	}

	protected override string GetClassNameCore()
	{
		return "FAInfoBar";
	}

	internal void RaiseOpenedEvent(FAInfoBarSeverity severity, string displayString)
	{
	}

	internal void RaiseClosedEvent(FAInfoBarSeverity severity, string displayString)
	{
	}
}
