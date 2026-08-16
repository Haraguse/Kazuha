using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace FluentAvalonia.UI.Controls;

internal class BreadcrumbElementFactory : FAElementFactory
{
	private IFAElementFactory _itemTemplateWrapper;

	public void UserElementFactory(object newValue)
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
		else if (newValue is IFAElementFactory itemTemplateWrapper)
		{
			_itemTemplateWrapper = itemTemplateWrapper;
		}
	}

	protected override Control GetElementCore(FAElementFactoryGetArgs args)
	{
		object obj = GetNewContent(_itemTemplateWrapper, args);
		if (obj is FABreadcrumbBarItem fABreadcrumbBarItem)
		{
			((ContentControl)fABreadcrumbBarItem).Content = args.Data;
			return (Control)(object)fABreadcrumbBarItem;
		}
		FABreadcrumbBarItem fABreadcrumbBarItem2 = new FABreadcrumbBarItem();
		((ContentControl)fABreadcrumbBarItem2).Content = obj;
		FABreadcrumbBarItem fABreadcrumbBarItem3 = fABreadcrumbBarItem2;
		if (_itemTemplateWrapper is FAItemTemplateWrapper fAItemTemplateWrapper)
		{
			((ContentControl)fABreadcrumbBarItem3).ContentTemplate = fAItemTemplateWrapper.Template;
		}
		return (Control)(object)fABreadcrumbBarItem3;
		static object GetNewContent(IFAElementFactory factory, FAElementFactoryGetArgs fAElementFactoryGetArgs)
		{
			if (fAElementFactoryGetArgs.Data is FABreadcrumbBarItem result)
			{
				return result;
			}
			if (factory != null)
			{
				return factory.GetElement(fAElementFactoryGetArgs);
			}
			return fAElementFactoryGetArgs.Data;
		}
	}

	protected override void RecycleElementCore(FAElementFactoryRecycleArgs args)
	{
		Control element = args.Element;
		if (element != null)
		{
			bool flag = false;
			if (element is FABreadcrumbBarItem fABreadcrumbBarItem)
			{
				fABreadcrumbBarItem.ResetVisualProperties();
				flag = fABreadcrumbBarItem.IsEllipsisDropDownItem();
			}
			if ((_itemTemplateWrapper != null) & flag)
			{
				_itemTemplateWrapper.RecycleElement(args);
			}
		}
	}
}
