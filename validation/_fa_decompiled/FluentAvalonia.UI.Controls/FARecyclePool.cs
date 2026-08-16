using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace FluentAvalonia.UI.Controls;

public class FARecyclePool
{
	private struct ElementInfo(Control element, Panel owner)
	{
		public Control Element = element;

		public Panel Owner = owner;
	}

	public static readonly AttachedProperty<IDataTemplate> OriginTemplateProperty = AvaloniaProperty.RegisterAttached<FARecyclePool, Control, IDataTemplate>("OriginTemplate", (IDataTemplate)null, false, (BindingMode)1, (Func<IDataTemplate, bool>)null, (Func<AvaloniaObject, IDataTemplate, IDataTemplate>)null);

	public static readonly AttachedProperty<string> ReuseKeyProperty = AvaloniaProperty.RegisterAttached<FARecyclePool, Control, string>("ReuseKey", (string)null, false, (BindingMode)1, (Func<string, bool>)null, (Func<AvaloniaObject, string, string>)null);

	private readonly Dictionary<string, List<ElementInfo>> _elements = new Dictionary<string, List<ElementInfo>>();

	private static Dictionary<IDataTemplate, FARecyclePool> s_PoolInstance;

	public static string GetReuseKey(Control element)
	{
		return ((AvaloniaObject)element).GetValue<string>((StyledProperty<string>)(object)ReuseKeyProperty);
	}

	public static void SetReuseKey(Control element, string key)
	{
		((AvaloniaObject)element).SetValue<string>((StyledProperty<string>)(object)ReuseKeyProperty, key, (BindingPriority)0);
	}

	public void PutElement(Control element, string key)
	{
		PutElementCore(element, key, null);
	}

	public void PutElement(Control element, string key, Control owner)
	{
		PutElementCore(element, key, owner);
	}

	public Control TryGetElement(string key)
	{
		return TryGetElementCore(key, null);
	}

	public Control TryGetElement(string key, Control owner)
	{
		return TryGetElementCore(key, owner);
	}

	protected virtual void PutElementCore(Control element, string key, Control owner)
	{
		EnsureOwnerIsPanelOrNull(owner);
		ElementInfo item = new ElementInfo(element, (Panel)(object)((owner is Panel) ? owner : null));
		if (_elements.TryGetValue(key, out var value))
		{
			value.Add(item);
			return;
		}
		List<ElementInfo> list = new List<ElementInfo>();
		list.Add(item);
		_elements.Add(key, list);
	}

	protected virtual Control TryGetElementCore(string key, Control owner)
	{
		if (_elements.TryGetValue(key, out var value) && value.Count > 0)
		{
			ElementInfo elementInfo = default(ElementInfo);
			bool flag = false;
			for (int i = 0; i < value.Count; i++)
			{
				ElementInfo elementInfo2 = value[i];
				if ((object)elementInfo2.Owner == owner || elementInfo2.Owner == null)
				{
					elementInfo = elementInfo2;
					value.RemoveAt(i);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				elementInfo = value[value.Count - 1];
				value.RemoveAt(value.Count - 1);
			}
			EnsureOwnerIsPanelOrNull(owner);
			if (elementInfo.Owner != null && (object)elementInfo.Owner != owner)
			{
				Panel owner2 = elementInfo.Owner;
				if (owner2 != null && !((AvaloniaList<Control>)(object)owner2.Children).Remove(elementInfo.Element))
				{
					throw new Exception("ItemsRepeater's child not found in its Children collection.");
				}
			}
			return elementInfo.Element;
		}
		return null;
	}

	private void EnsureOwnerIsPanelOrNull(Control owner)
	{
		if (owner == null || (owner != null && owner is Panel))
		{
			return;
		}
		throw new InvalidOperationException("Owner must to be a Panel or null.");
	}

	public static FARecyclePool GetPoolInstance(IDataTemplate template)
	{
		if (s_PoolInstance == null)
		{
			s_PoolInstance = new Dictionary<IDataTemplate, FARecyclePool>();
		}
		if (s_PoolInstance.TryGetValue(template, out var value))
		{
			return value;
		}
		return null;
	}

	public static void SetPoolInstance(IDataTemplate template, FARecyclePool pool)
	{
		if (s_PoolInstance == null)
		{
			s_PoolInstance = new Dictionary<IDataTemplate, FARecyclePool>();
		}
		s_PoolInstance.Add(template, pool);
	}
}
