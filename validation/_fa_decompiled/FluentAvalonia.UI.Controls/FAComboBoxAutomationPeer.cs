using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// AutomationPeer for the FAComboBox control
/// </summary>
public class FAComboBoxAutomationPeer : SelectingItemsControlAutomationPeer, IExpandCollapseProvider, IValueProvider
{
	private class UnrealizedSelectionPeer : UnrealizedElementAutomationPeer
	{
		private readonly FAComboBoxAutomationPeer _owner;

		private object _item;

		public object Item
		{
			get
			{
				return _item;
			}
			set
			{
				if (_item != value)
				{
					string nameCore = ((AutomationPeer)this).GetNameCore();
					_item = value;
					((AutomationPeer)this).RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, (object)nameCore, (object)((AutomationPeer)this).GetNameCore());
				}
			}
		}

		public UnrealizedSelectionPeer(FAComboBoxAutomationPeer owner)
		{
			_owner = owner;
		}

		protected override string GetAcceleratorKeyCore()
		{
			return null;
		}

		protected override string GetAccessKeyCore()
		{
			return null;
		}

		protected override string GetAutomationIdCore()
		{
			return null;
		}

		protected override string GetClassNameCore()
		{
			return typeof(FAComboBoxItem).Name;
		}

		protected override AutomationPeer GetLabeledByCore()
		{
			return null;
		}

		protected override AutomationPeer GetParentCore()
		{
			return (AutomationPeer)(object)_owner;
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return (AutomationControlType)9;
		}

		protected override string GetNameCore()
		{
			object item = _item;
			Control val = (Control)((item is Control) ? item : null);
			if (val != null)
			{
				string text = AutomationProperties.GetName((StyledElement)(object)val);
				if (text == null)
				{
					ContentControl val2 = (ContentControl)(object)((val is ContentControl) ? val : null);
					if (val2 != null)
					{
						ContentPresenter presenter = val2.Presenter;
						Control obj = ((presenter != null) ? presenter.Child : null);
						TextBlock val3 = (TextBlock)(object)((obj is TextBlock) ? obj : null);
						if (val3 != null)
						{
							text = val3.Text;
						}
					}
				}
				if (text == null)
				{
					text = ((AvaloniaObject)val).GetValue<object>(ContentControl.ContentProperty)?.ToString();
				}
				return text;
			}
			return _item?.ToString();
		}
	}

	private UnrealizedSelectionPeer[] _selection;

	public FAComboBox Owner => (FAComboBox)(object)((ItemsControlAutomationPeer)this).Owner;

	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			return ToState(Owner.IsDropDownOpen);
		}
	}

	public bool ShowsMenu => true;

	bool IValueProvider.IsReadOnly => true;

	string IValueProvider.Value
	{
		get
		{
			IReadOnlyList<AutomationPeer> selection = ((SelectingItemsControlAutomationPeer)this).GetSelection();
			if (selection.Count != 1)
			{
				return null;
			}
			return selection[0].GetName();
		}
	}

	public FAComboBoxAutomationPeer(SelectingItemsControl owner)
		: base(owner)
	{
	}

	public void Collapse()
	{
		Owner.IsDropDownOpen = false;
	}

	public void Expand()
	{
		Owner.IsDropDownOpen = true;
	}

	public void SetValue(string value)
	{
		throw new NotSupportedException();
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return (AutomationControlType)4;
	}

	protected override IReadOnlyList<AutomationPeer> GetSelectionCore()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)ExpandCollapseState == 1)
		{
			return ((SelectingItemsControlAutomationPeer)this).GetSelectionCore();
		}
		object selectedItem = ((SelectingItemsControl)Owner).SelectedItem;
		if (selectedItem != null)
		{
			if (_selection == null)
			{
				_selection = new UnrealizedSelectionPeer[1]
				{
					new UnrealizedSelectionPeer(this)
				};
			}
			_selection[0].Item = selectedItem;
			return (IReadOnlyList<AutomationPeer>)(object)_selection;
		}
		return null;
	}

	protected override void OwnerPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		((SelectingItemsControlAutomationPeer)this).OwnerPropertyChanged(sender, e);
		if (e.Property == (AvaloniaProperty)(object)FAComboBox.IsDropDownOpenProperty)
		{
			((AutomationPeer)this).RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, (object)ToState((bool)e.OldValue), (object)ToState((bool)e.NewValue));
		}
	}

	private static ExpandCollapseState ToState(bool value)
	{
		if (value)
		{
			return (ExpandCollapseState)1;
		}
		return (ExpandCollapseState)0;
	}
}
