using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// A collection of CompositionVisual objects
/// </summary>
public class CompositionVisualCollection : CompositionObject, ServerListProxyHelper<CompositionVisual, ServerCompositionVisual>.IRegisterForSerialization, IList<CompositionVisual>, ICollection<CompositionVisual>, IEnumerable<CompositionVisual>, IEnumerable
{
	private readonly CompositionVisual _owner;

	private ServerListProxyHelper<CompositionVisual, ServerCompositionVisual> _list;

	public int Count => _list.Count;

	public bool IsReadOnly => _list.IsReadOnly;

	public CompositionVisual this[int index]
	{
		get
		{
			return _list[index];
		}
		set
		{
			CompositionVisual oldItem = _list[index];
			OnBeforeReplace(oldItem, value);
			_list[index] = value;
			OnReplace(oldItem, value);
		}
	}

	internal CompositionVisualCollection(CompositionVisual parent, ServerCompositionVisualCollection server)
		: base(parent.Compositor, server)
	{
		_owner = parent;
		InitializeDefaults();
	}

	public void InsertAbove(CompositionVisual newChild, CompositionVisual sibling)
	{
		int num = _list.IndexOf(sibling);
		if (num == -1)
		{
			throw new InvalidOperationException();
		}
		Insert(num + 1, newChild);
	}

	public void InsertBelow(CompositionVisual newChild, CompositionVisual sibling)
	{
		int num = _list.IndexOf(sibling);
		if (num == -1)
		{
			throw new InvalidOperationException();
		}
		Insert(num, newChild);
	}

	public void InsertAtTop(CompositionVisual newChild)
	{
		Insert(_list.Count, newChild);
	}

	public void InsertAtBottom(CompositionVisual newChild)
	{
		Insert(0, newChild);
	}

	public void RemoveAll()
	{
		Clear();
	}

	private void AddHitTestChild(CompositionVisual item)
	{
		if (_owner is CompositionContainerVisual compositionContainerVisual)
		{
			compositionContainerVisual.AddHitTestChild(item);
		}
	}

	private void RemoveHitTestChild(CompositionVisual item)
	{
		if (_owner is CompositionContainerVisual compositionContainerVisual)
		{
			compositionContainerVisual.RemoveHitTestChild(item);
		}
	}

	private void ClearHitTestChildren()
	{
		if (_owner is CompositionContainerVisual compositionContainerVisual)
		{
			compositionContainerVisual.ClearHitTestChildren();
		}
	}

	private void InitializeDefaults()
	{
		_list = new ServerListProxyHelper<CompositionVisual, ServerCompositionVisual>(this);
	}

	void ServerListProxyHelper<CompositionVisual, ServerCompositionVisual>.IRegisterForSerialization.RegisterForSerialization()
	{
		RegisterForSerialization();
	}

	public List<CompositionVisual>.Enumerator GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	IEnumerator<CompositionVisual> IEnumerable<CompositionVisual>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_list).GetEnumerator();
	}

	public void Add(CompositionVisual item)
	{
		OnBeforeAdded(item);
		_list.Add(item);
		OnAdded(item);
	}

	public void Clear()
	{
		OnBeforeClear();
		_list.Clear();
		OnClear();
	}

	public bool Contains(CompositionVisual item)
	{
		return _list.Contains(item);
	}

	public void CopyTo(CompositionVisual[] array, int arrayIndex)
	{
		_list.CopyTo(array, arrayIndex);
	}

	public bool Remove(CompositionVisual item)
	{
		bool num = _list.Remove(item);
		if (num)
		{
			OnRemoved(item);
		}
		return num;
	}

	public int IndexOf(CompositionVisual item)
	{
		return _list.IndexOf(item);
	}

	public void Insert(int index, CompositionVisual item)
	{
		OnBeforeAdded(item);
		_list.Insert(index, item);
		OnAdded(item);
	}

	public void RemoveAt(int index)
	{
		CompositionVisual item = _list[index];
		_list.RemoveAt(index);
		OnRemoved(item);
	}

	private void OnBeforeAdded(CompositionVisual item)
	{
		if (item.Parent != null)
		{
			throw new InvalidOperationException("Visual already has a parent");
		}
		item.Parent = _owner;
	}

	private void OnAdded(CompositionVisual item)
	{
		item.Parent = _owner;
		AddHitTestChild(item);
	}

	private void OnRemoved(CompositionVisual item)
	{
		item.Parent = null;
		RemoveHitTestChild(item);
	}

	private void OnBeforeClear()
	{
		using List<CompositionVisual>.Enumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			enumerator.Current.Parent = null;
		}
	}

	private void OnBeforeReplace(CompositionVisual oldItem, CompositionVisual newItem)
	{
		if (oldItem != newItem)
		{
			OnBeforeAdded(newItem);
		}
	}

	private void OnReplace(CompositionVisual oldItem, CompositionVisual newItem)
	{
		if (oldItem != newItem)
		{
			OnRemoved(oldItem);
			OnAdded(newItem);
		}
	}

	private void OnClear()
	{
		ClearHitTestChildren();
	}

	private protected override void SerializeChangesCore(BatchStreamWriter writer)
	{
		_list.Serialize(writer);
		base.SerializeChangesCore(writer);
	}
}
