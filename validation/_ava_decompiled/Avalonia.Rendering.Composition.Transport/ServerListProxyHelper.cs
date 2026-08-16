using System.Collections;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Transport;

/// <summary>
/// A helper class used from generated UI-thread-side collections of composition objects.
/// </summary>
internal class ServerListProxyHelper<TClient, TServer> : IList<TClient>, ICollection<TClient>, IEnumerable<TClient>, IEnumerable where TClient : CompositionObject where TServer : ServerObject
{
	public interface IRegisterForSerialization
	{
		void RegisterForSerialization();
	}

	private readonly IRegisterForSerialization _parent;

	private bool _changed;

	private readonly List<TClient> _list = new List<TClient>();

	public int Count => _list.Count;

	public bool IsReadOnly => false;

	public TClient this[int index]
	{
		get
		{
			return _list[index];
		}
		set
		{
			_list[index] = value;
			_changed = true;
			_parent.RegisterForSerialization();
		}
	}

	public ServerListProxyHelper(IRegisterForSerialization parent)
	{
		_parent = parent;
	}

	IEnumerator<TClient> IEnumerable<TClient>.GetEnumerator()
	{
		return GetEnumerator();
	}

	public List<TClient>.Enumerator GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Add(TClient item)
	{
		Insert(_list.Count, item);
	}

	public void Clear()
	{
		_list.Clear();
		_changed = true;
		_parent.RegisterForSerialization();
	}

	public bool Contains(TClient item)
	{
		return _list.Contains(item);
	}

	public void CopyTo(TClient[] array, int arrayIndex)
	{
		_list.CopyTo(array, arrayIndex);
	}

	public bool Remove(TClient item)
	{
		int num = _list.IndexOf(item);
		if (num == -1)
		{
			return false;
		}
		RemoveAt(num);
		return true;
	}

	public int IndexOf(TClient item)
	{
		return _list.IndexOf(item);
	}

	public void Insert(int index, TClient item)
	{
		_list.Insert(index, item);
		_changed = true;
		_parent.RegisterForSerialization();
	}

	public void RemoveAt(int index)
	{
		_list.RemoveAt(index);
		_changed = true;
		_parent.RegisterForSerialization();
	}

	public void Serialize(BatchStreamWriter writer)
	{
		writer.Write(_changed ? ((byte)1) : ((byte)0));
		if (_changed)
		{
			writer.Write(_list.Count);
			foreach (TClient item in _list)
			{
				writer.WriteObject(item.Server);
			}
		}
		_changed = false;
	}
}
