using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

public class FANumberBoxAutomationPeer : ControlAutomationPeer, IRangeValueProvider
{
	public bool IsReadOnly { get; }

	public double Minimum => GetImpl().Minimum;

	public double Maximum => GetImpl().Maximum;

	public double Value => GetImpl().Value;

	public double LargeChange => GetImpl().LargeChange;

	public double SmallChange => GetImpl().SmallChange;

	public FANumberBoxAutomationPeer(Control owner)
		: base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return (AutomationControlType)18;
	}

	protected override string GetNameCore()
	{
		string text = ((ControlAutomationPeer)this).GetNameCore();
		if (string.IsNullOrEmpty(text) && ((ControlAutomationPeer)this).Owner is FANumberBox fANumberBox)
		{
			text = ((fANumberBox.Header is string) ? fANumberBox.Header.ToString() : null);
		}
		return text;
	}

	public void SetValue(double value)
	{
		GetImpl().Value = value;
	}

	internal void RaiseValueChangedEvent(double oldValue, double newValue)
	{
		((AutomationPeer)this).RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, (object)oldValue, (object)newValue);
	}

	private FANumberBox GetImpl()
	{
		return (FANumberBox)(object)((ControlAutomationPeer)this).Owner;
	}
}
