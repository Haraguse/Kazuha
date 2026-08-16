using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

public class FAItemTemplateWrapper : IFAElementFactory, IDataTemplate, ITemplate<object?, Control?>
{
	private IDataTemplate _dataTemplate;

	private FADataTemplateSelector _dataTemplateSelector;

	public IDataTemplate Template
	{
		get
		{
			return _dataTemplate;
		}
		set
		{
			_dataTemplate = value;
		}
	}

	public FADataTemplateSelector TemplateSelector
	{
		get
		{
			return _dataTemplateSelector;
		}
		set
		{
			_dataTemplateSelector = value;
		}
	}

	public FAItemTemplateWrapper(IDataTemplate template)
	{
		_dataTemplate = template;
	}

	public FAItemTemplateWrapper(FADataTemplateSelector selector)
	{
		_dataTemplateSelector = selector;
	}

	public Control GetElement(FAElementFactoryGetArgs args)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		IDataTemplate val = _dataTemplate ?? _dataTemplateSelector?.SelectTemplate(args.Data);
		if (val == null)
		{
			try
			{
				val = _dataTemplateSelector.SelectTemplate(args.Data, null);
			}
			catch
			{
			}
			if (val == null)
			{
				throw new InvalidOperationException("Null encountered as data template. That is not a valid value for a data template, and can not be used.");
			}
		}
		FARecyclePool poolInstance = FARecyclePool.GetPoolInstance(val);
		Control val2 = null;
		if (poolInstance != null)
		{
			val2 = poolInstance.TryGetElement(string.Empty, args.Parent);
		}
		if (val2 == null)
		{
			val2 = ((ITemplate<object, Control>)(object)val).Build(args.Data);
			if (val2 == null)
			{
				val2 = (Control)new Rectangle();
			}
			((AvaloniaObject)val2).SetValue<IDataTemplate>((StyledProperty<IDataTemplate>)(object)FARecyclePool.OriginTemplateProperty, val, (BindingPriority)0);
		}
		return val2;
	}

	public void RecycleElement(FAElementFactoryRecycleArgs args)
	{
		Control element = args.Element;
		IDataTemplate template = _dataTemplate ?? ((AvaloniaObject)element).GetValue<IDataTemplate>((StyledProperty<IDataTemplate>)(object)FARecyclePool.OriginTemplateProperty);
		FARecyclePool fARecyclePool = FARecyclePool.GetPoolInstance(template);
		if (fARecyclePool == null)
		{
			fARecyclePool = new FARecyclePool();
			FARecyclePool.SetPoolInstance(template, fARecyclePool);
		}
		fARecyclePool.PutElement(args.Element, string.Empty, args.Parent);
	}

	bool IDataTemplate.Match(object data)
	{
		throw new NotImplementedException();
	}

	Control ITemplate<object, Control>.Build(object param)
	{
		throw new NotImplementedException();
	}
}
