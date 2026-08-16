using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

public sealed class FANavigationViewAutomationPeer : ControlAutomationPeer, ISelectionProvider
{
	public bool CanSelectMultiple => false;

	public bool IsSelectionRequired => false;

	public FANavigationViewAutomationPeer(Control owner)
		: base(owner)
	{
	}

	public IReadOnlyList<AutomationPeer> GetSelection()
	{
		if (((ControlAutomationPeer)this).Owner is FANavigationView fANavigationView)
		{
			AutomationPeer val = ControlAutomationPeer.CreatePeerForElement((Control)(object)fANavigationView.GetSelectedContainer());
			return (IReadOnlyList<AutomationPeer>)(object)new AutomationPeer[1] { val };
		}
		return null;
	}

	internal void RaiseSelectionChangedEvent(object oldSelection, object newSelection)
	{
		if (((ControlAutomationPeer)this).Owner is FANavigationView fANavigationView)
		{
			FANavigationViewItem selectedContainer = fANavigationView.GetSelectedContainer();
			if (selectedContainer != null)
			{
				ControlAutomationPeer.CreatePeerForElement((Control)(object)selectedContainer).RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, oldSelection, newSelection);
			}
		}
	}
}
