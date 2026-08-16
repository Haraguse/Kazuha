using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Element factory for the ItemsRepeaters in a NavigationView
/// </summary>
internal class NavigationViewItemsFactory : FAElementFactory
{
	private IFAElementFactory _itemTemplateWrapper;

	private FANavigationViewItemBase _settingsItem;

	private List<FANavigationViewItem> _navViewPool;

	public FANavigationViewItemBase SettingsItem
	{
		set
		{
			_settingsItem = value;
		}
	}

	public void UserElementFactory(object newValue)
	{
		_itemTemplateWrapper = newValue as IFAElementFactory;
		if (_itemTemplateWrapper == null)
		{
			IDataTemplate val = (IDataTemplate)((newValue is IDataTemplate) ? newValue : null);
			if (val != null)
			{
				_itemTemplateWrapper = new FAItemTemplateWrapper(val);
			}
			else if (newValue is FADataTemplateSelector selector)
			{
				_itemTemplateWrapper = new FAItemTemplateWrapper(selector);
			}
		}
		_navViewPool = new List<FANavigationViewItem>(4);
	}

	protected override Control GetElementCore(FAElementFactoryGetArgs args)
	{
		object obj = args.Data;
		if (_settingsItem != null && _settingsItem == args.Data)
		{
			return (Control)(object)(args.Data as FANavigationViewItem);
		}
		if (_itemTemplateWrapper != null)
		{
			obj = _itemTemplateWrapper.GetElement(args);
		}
		if (obj is FANavigationViewItemBase result)
		{
			return (Control)(object)result;
		}
		if (_navViewPool == null)
		{
			_navViewPool = new List<FANavigationViewItem>();
		}
		FANavigationViewItem fANavigationViewItem;
		if (_navViewPool.Count > 0)
		{
			fANavigationViewItem = _navViewPool[_navViewPool.Count - 1];
			_navViewPool.RemoveAt(_navViewPool.Count - 1);
		}
		else
		{
			fANavigationViewItem = new FANavigationViewItem();
		}
		fANavigationViewItem.CreatedByNavigationViewItemsFactory = true;
		if (_itemTemplateWrapper != null && _itemTemplateWrapper is FAItemTemplateWrapper contentTemplate)
		{
			FAElementFactoryRecycleArgs fAElementFactoryRecycleArgs = new FAElementFactoryRecycleArgs();
			fAElementFactoryRecycleArgs.Element = (Control)((obj is Control) ? obj : null);
			_itemTemplateWrapper.RecycleElement(fAElementFactoryRecycleArgs);
			((ContentControl)fANavigationViewItem).Content = args.Data;
			((ContentControl)fANavigationViewItem).ContentTemplate = (IDataTemplate)(object)contentTemplate;
			return (Control)(object)fANavigationViewItem;
		}
		((ContentControl)fANavigationViewItem).Content = obj;
		return (Control)(object)fANavigationViewItem;
	}

	protected override void RecycleElementCore(FAElementFactoryRecycleArgs args)
	{
		if (args.Element != null)
		{
			if (args.Element is FANavigationViewItem { CreatedByNavigationViewItemsFactory: not false } fANavigationViewItem)
			{
				fANavigationViewItem.CreatedByNavigationViewItemsFactory = false;
				UnlinkElementFromParent(args);
				args.Element = null;
				_navViewPool.Add(fANavigationViewItem);
				_ = _itemTemplateWrapper;
			}
			bool flag = _settingsItem != null && (object)_settingsItem == args.Element;
			if (_itemTemplateWrapper != null && !flag)
			{
				_itemTemplateWrapper.RecycleElement(args);
			}
			else
			{
				UnlinkElementFromParent(args);
			}
		}
	}

	private void UnlinkElementFromParent(FAElementFactoryRecycleArgs args)
	{
		Control parent = args.Parent;
		Panel val = (Panel)(object)((parent is Panel) ? parent : null);
		if (val != null)
		{
			((AvaloniaList<Control>)(object)val.Children).Remove(args.Element);
		}
	}
}
