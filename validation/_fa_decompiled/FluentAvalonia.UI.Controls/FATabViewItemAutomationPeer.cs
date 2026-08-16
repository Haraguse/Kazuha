using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Represents the <see cref="T:Avalonia.Automation.Peers.AutomationPeer" /> for a <see cref="T:FluentAvalonia.UI.Controls.FATabViewItem" />
/// </summary>
public sealed class FATabViewItemAutomationPeer : ListItemAutomationPeer, ISelectionItemProvider
{
	bool ISelectionItemProvider.IsSelected => (((ContentControlAutomationPeer)this).Owner as FATabViewItem)?.IsSelected ?? false;

	ISelectionProvider ISelectionItemProvider.SelectionContainer
	{
		get
		{
			FATabView parentTabView = GetParentTabView();
			if (parentTabView != null)
			{
				AutomationPeer obj = ControlAutomationPeer.CreatePeerForElement((Control)(object)parentTabView);
				return (ISelectionProvider)(object)((obj is ISelectionProvider) ? obj : null);
			}
			return null;
		}
	}

	public FATabViewItemAutomationPeer(ContentControl owner)
		: base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return (AutomationControlType)21;
	}

	protected override string GetNameCore()
	{
		string text = ((ContentControlAutomationPeer)this).GetNameCore();
		if (string.IsNullOrEmpty(text) && ((ContentControlAutomationPeer)this).Owner is FATabViewItem { Header: var header })
		{
			text = header?.ToString() ?? "TabViewItem";
		}
		return text;
	}

	void ISelectionItemProvider.AddToSelection()
	{
		((ISelectionItemProvider)this).Select();
	}

	void ISelectionItemProvider.RemoveFromSelection()
	{
	}

	void ISelectionItemProvider.Select()
	{
		if (((ContentControlAutomationPeer)this).Owner is FATabViewItem fATabViewItem)
		{
			fATabViewItem.IsSelected = true;
		}
	}

	private FATabView GetParentTabView()
	{
		if (((ContentControlAutomationPeer)this).Owner is FATabViewItem fATabViewItem)
		{
			return fATabViewItem.ParentTabView ?? VisualExtensions.FindAncestorOfType<FATabView>((Visual)(object)fATabViewItem, false);
		}
		return null;
	}
}
