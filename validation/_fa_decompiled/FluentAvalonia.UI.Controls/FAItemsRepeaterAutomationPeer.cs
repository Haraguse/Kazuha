using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace FluentAvalonia.UI.Controls;

public class FAItemsRepeaterAutomationPeer : ControlAutomationPeer
{
	public FAItemsRepeater Owner => (FAItemsRepeater)(object)((ControlAutomationPeer)this).Owner;

	public FAItemsRepeaterAutomationPeer(Control owner)
		: base(owner)
	{
	}

	protected override IReadOnlyList<AutomationPeer> GetChildrenCore()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		FAItemsRepeater owner = Owner;
		IReadOnlyList<AutomationPeer> childrenCore = ((ControlAutomationPeer)this).GetChildrenCore();
		int count = childrenCore.Count;
		List<(int, AutomationPeer)> list = new List<(int, AutomationPeer)>(count);
		for (int i = 0; i < count; i++)
		{
			AutomationPeer val = childrenCore[i];
			Control element = GetElement((ControlAutomationPeer)val, owner);
			if (element != null)
			{
				VirtualizationInfo virtualizationInfo = FAItemsRepeater.GetVirtualizationInfo(element);
				if (virtualizationInfo != null && virtualizationInfo.IsRealized)
				{
					list.Add((virtualizationInfo.Index, val));
				}
			}
		}
		list.Sort(((int, AutomationPeer) lhs, (int, AutomationPeer) rhs) => (lhs.Item1 < rhs.Item1) ? 1 : (-1));
		return list.Select(((int, AutomationPeer) x) => x.Item2).ToArray();
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return (AutomationControlType)28;
	}

	private static Control GetElement(ControlAutomationPeer childPeer, FAItemsRepeater repeater)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		Control val = childPeer.Owner;
		Visual visualParent = VisualExtensions.GetVisualParent((Visual)(object)val);
		while (visualParent != null && visualParent as FAItemsRepeater != repeater)
		{
			val = (Control)visualParent;
			visualParent = VisualExtensions.GetVisualParent((Visual)(object)val);
		}
		return val;
	}
}
