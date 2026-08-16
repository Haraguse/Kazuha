using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

internal class UniqueIdElementPool
{
	private readonly FAItemsRepeater _owner;

	private readonly Dictionary<string, Control> _elementMap = new Dictionary<string, Control>();

	public UniqueIdElementPool(FAItemsRepeater ir)
	{
		_owner = ir;
	}

	public void Add(Control element)
	{
		string uniqueId = FAItemsRepeater.GetVirtualizationInfo(element).UniqueId;
		if (_elementMap.ContainsKey(uniqueId))
		{
			throw new InvalidOperationException("The ID is not unique");
		}
		_elementMap.Add(uniqueId, element);
	}

	public Control Remove(int index)
	{
		Control value = null;
		string key = _owner.ItemsSourceView.KeyFromIndex(index);
		if (_elementMap.TryGetValue(key, out value))
		{
			_elementMap.Remove(key);
		}
		return value;
	}

	public void Clear()
	{
		_elementMap.Clear();
	}

	public IEnumerator<KeyValuePair<string, Control>> GetEnumerator()
	{
		return _elementMap.GetEnumerator();
	}
}
