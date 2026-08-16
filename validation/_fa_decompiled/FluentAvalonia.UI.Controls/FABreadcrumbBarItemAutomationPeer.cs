using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Defines the automation peer for a <see cref="T:FluentAvalonia.UI.Controls.FABreadcrumbBarItem" />
/// </summary>
public class FABreadcrumbBarItemAutomationPeer : ControlAutomationPeer, IInvokeProvider
{
	public FABreadcrumbBarItemAutomationPeer(Control owner)
		: base(owner)
	{
	}

	protected override string GetLocalizedControlTypeCore()
	{
		return FALocalizationHelper.Instance.GetLocalizedStringResource("BreadcrumbBarItemLocalizedControlType");
	}

	protected override string GetClassNameCore()
	{
		return "FABreadcrumbBarItem";
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return (AutomationControlType)1;
	}

	private FABreadcrumbBarItem GetImpl()
	{
		return ((ControlAutomationPeer)this).Owner as FABreadcrumbBarItem;
	}

	void IInvokeProvider.Invoke()
	{
		GetImpl()?.OnClickEvent(null, null);
	}
}
