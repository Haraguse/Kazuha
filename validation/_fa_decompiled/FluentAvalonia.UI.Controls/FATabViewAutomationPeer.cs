using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represets the <see cref="T:Avalonia.Automation.Peers.AutomationPeer" /> for a <see cref="T:FluentAvalonia.UI.Controls.FATabView" />
/// </summary>
public sealed class FATabViewAutomationPeer : ControlAutomationPeer, ISelectionProvider
{
	public bool CanSelectMultiple => false;

	public bool IsSelectionRequired => true;

	public FATabViewAutomationPeer(Control owner)
		: base(owner)
	{
	}

	protected override string GetClassNameCore()
	{
		return "FATabView";
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return (AutomationControlType)20;
	}

	public IReadOnlyList<AutomationPeer> GetSelection()
	{
		if (((ControlAutomationPeer)this).Owner is FATabView fATabView && fATabView.ContainerFromIndex(fATabView.SelectedIndex) is FATabViewItem fATabViewItem)
		{
			return (IReadOnlyList<AutomationPeer>)(object)new AutomationPeer[1] { ControlAutomationPeer.CreatePeerForElement((Control)(object)fATabViewItem) };
		}
		return Array.Empty<AutomationPeer>();
	}
}
