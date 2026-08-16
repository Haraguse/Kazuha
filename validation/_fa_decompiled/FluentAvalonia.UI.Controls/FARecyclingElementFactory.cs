using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

public class FARecyclingElementFactory : FAElementFactory
{
	private FASelectTemplateEventArgs _args;

	public FARecyclePool RecyclePool { get; set; }

	public IDictionary<string, IDataTemplate> Templates { get; set; }

	public event TypedEventHandler<FARecyclingElementFactory, FASelectTemplateEventArgs> SelectTemplateKey;

	public FARecyclingElementFactory()
	{
		Templates = new Dictionary<string, IDataTemplate>();
	}

	protected virtual string OnSelectTemplateKeyCore(object dataContext, Control owner)
	{
		if (_args == null)
		{
			_args = new FASelectTemplateEventArgs();
		}
		_args.TemplateKey = null;
		_args.DataContext = dataContext;
		_args.Owner = owner;
		SelectTemplateKey?.Invoke(this, _args);
		string templateKey = _args.TemplateKey;
		if (string.IsNullOrEmpty(templateKey))
		{
			throw new InvalidOperationException("Please provide a valid template identifier in the handler for the SelectTemplateKey event.");
		}
		return templateKey;
	}

	protected override Control GetElementCore(FAElementFactoryGetArgs args)
	{
		if (Templates == null || Templates.Count == 0)
		{
			throw new InvalidOperationException("Templates property cannot be null or empty.");
		}
		Control parent = args.Parent;
		string text = ((Templates.Count == 1) ? Templates.First().Key : OnSelectTemplateKeyCore(args.Data, parent));
		if (string.IsNullOrEmpty(text))
		{
			throw new InvalidOperationException("Template key cannot be empty or null.");
		}
		Control val = RecyclePool.TryGetElement(text, parent);
		if (val == null)
		{
			if (Templates.Count > 1 && !Templates.ContainsKey(text))
			{
				throw new InvalidOperationException("No templates of key " + text + " were found in the templates collection");
			}
			val = ((ITemplate<object, Control>)(object)Templates[text]).Build(args.Data);
			FARecyclePool.SetReuseKey(val, text);
		}
		return val;
	}

	protected override void RecycleElementCore(FAElementFactoryRecycleArgs args)
	{
		Control element = args.Element;
		string reuseKey = FARecyclePool.GetReuseKey(element);
		RecyclePool.PutElement(element, reuseKey, args.Parent);
	}
}
