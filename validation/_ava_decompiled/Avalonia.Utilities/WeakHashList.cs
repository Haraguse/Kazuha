using System;
using System.Collections.Generic;
using Avalonia.Collections.Pooled;

namespace Avalonia.Utilities;

internal class WeakHashList<T> where T : class
{
	private struct Key
	{
		public WeakReference<T>? Weak;

		public T? Strong;

		public int HashCode;

		public static Key MakeStrong(T r)
		{
			return new Key
			{
				HashCode = r.GetHashCode(),
				Strong = r
			};
		}

		public static Key MakeWeak(T r)
		{
			return new Key
			{
				HashCode = r.GetHashCode(),
				Weak = new WeakReference<T>(r)
			};
		}

		public override int GetHashCode()
		{
			return HashCode;
		}
	}

	private class KeyComparer : IEqualityComparer<Key>
	{
		public static KeyComparer Instance = new KeyComparer();

		public bool Equals(Key x, Key y)
		{
			if (x.HashCode != y.HashCode)
			{
				return false;
			}
			if (x.Strong != null)
			{
				if (y.Strong != null)
				{
					return x.Strong == y.Strong;
				}
				if (y.Weak == null)
				{
					return false;
				}
				if (y.Weak.TryGetTarget(out var target))
				{
					return target == x.Strong;
				}
				return false;
			}
			if (y.Strong != null)
			{
				if (x.Weak == null)
				{
					return false;
				}
				if (x.Weak.TryGetTarget(out var target2))
				{
					return target2 == y.Strong;
				}
				return false;
			}
			if (x.Weak == null || !x.Weak.TryGetTarget(out var target3))
			{
				WeakReference<T>? weak = y.Weak;
				if (weak == null)
				{
					return true;
				}
				T target4;
				return !weak.TryGetTarget(out target4);
			}
			WeakReference<T>? weak2 = y.Weak;
			if (weak2 != null && weak2.TryGetTarget(out var target5))
			{
				return target3 == target5;
			}
			return false;
		}

		public int GetHashCode(Key obj)
		{
			return obj.HashCode;
		}
	}

	public const int DefaultArraySize = 8;

	private Dictionary<Key, int>? _dic;

	private WeakReference<T>?[]? _arr;

	private int _arrCount;

	private static readonly Stack<PooledList<T>> s_listPool = new Stack<PooledList<T>>();

	public bool IsEmpty
	{
		get
		{
			if (_dic == null)
			{
				return _arrCount == 0;
			}
			return _dic.Count == 0;
		}
	}

	public bool NeedCompact { get; private set; }

	public void Add(T item)
	{
		if (_dic != null)
		{
			Key key = Key.MakeStrong(item);
			if (_dic.TryGetValue(key, out var value))
			{
				_dic[key] = value + 1;
			}
			else
			{
				_dic[Key.MakeWeak(item)] = 1;
			}
			return;
		}
		if (_arr == null)
		{
			_arr = new WeakReference<T>[8];
		}
		if (_arrCount < _arr.Length)
		{
			_arr[_arrCount] = new WeakReference<T>(item);
			_arrCount++;
			return;
		}
		for (int i = 0; i < _arrCount; i++)
		{
			if (!_arr[i].TryGetTarget(out var _))
			{
				_arr[i] = new WeakReference<T>(item);
				return;
			}
		}
		_dic = new Dictionary<Key, int>(KeyComparer.Instance);
		WeakReference<T>[] arr = _arr;
		for (int j = 0; j < arr.Length; j++)
		{
			if (arr[j].TryGetTarget(out var target2))
			{
				Add(target2);
			}
		}
		Add(item);
		_arr = null;
		_arrCount = 0;
	}

	public void Remove(T item)
	{
		if (_arr != null)
		{
			for (int i = 0; i < _arrCount; i++)
			{
				WeakReference<T>? obj = _arr[i];
				if (obj != null && obj.TryGetTarget(out var target) && target == item)
				{
					_arr[i] = null;
					ArrCompact();
					break;
				}
			}
		}
		else if (_dic != null)
		{
			Key key = Key.MakeStrong(item);
			if (_dic.TryGetValue(key, out var value) && value > 1)
			{
				_dic[key] = value - 1;
			}
			else
			{
				_dic.Remove(key);
			}
		}
	}

	private void ArrCompact()
	{
		if (_arr == null)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < _arrCount; i++)
		{
			WeakReference<T> weakReference = _arr[i];
			if (weakReference == null && num == -1)
			{
				num = i;
			}
			if (weakReference != null && num != -1)
			{
				_arr[i] = null;
				_arr[num] = weakReference;
				num++;
			}
		}
		if (num != -1)
		{
			_arrCount = num;
		}
	}

	public void Compact()
	{
		if (_dic == null)
		{
			return;
		}
		PooledList<Key> pooledList = null;
		foreach (KeyValuePair<Key, int> item in _dic)
		{
			WeakReference<T>? weak = item.Key.Weak;
			if (weak == null || !weak.TryGetTarget(out var _))
			{
				(pooledList ?? (pooledList = new PooledList<Key>())).Add(item.Key);
			}
		}
		if (pooledList == null)
		{
			return;
		}
		foreach (Key item2 in pooledList)
		{
			_dic.Remove(item2);
		}
		pooledList.Dispose();
	}

	public static void ReturnToSharedPool(PooledList<T> list)
	{
		list.Clear();
		s_listPool.Push(list);
	}

	public PooledList<T>? GetAlive(Func<PooledList<T>>? factory = null)
	{
		PooledList<T> pooledList = null;
		if (_arr != null)
		{
			bool flag = false;
			for (int i = 0; i < _arrCount; i++)
			{
				WeakReference<T>? obj = _arr[i];
				if (obj != null && obj.TryGetTarget(out var target))
				{
					object obj2 = pooledList;
					if (obj2 == null)
					{
						obj2 = factory?.Invoke() ?? ((s_listPool.Count > 0) ? s_listPool.Pop() : new PooledList<T>());
						pooledList = (PooledList<T>)obj2;
					}
					((PooledList<T>)obj2).Add(target);
				}
				else
				{
					_arr[i] = null;
					flag = true;
				}
			}
			if (flag)
			{
				ArrCompact();
			}
			return pooledList;
		}
		if (_dic != null)
		{
			foreach (KeyValuePair<Key, int> item in _dic)
			{
				WeakReference<T>? weak = item.Key.Weak;
				if (weak != null && weak.TryGetTarget(out var target2))
				{
					object obj3 = pooledList;
					if (obj3 == null)
					{
						obj3 = factory?.Invoke() ?? ((s_listPool.Count > 0) ? s_listPool.Pop() : new PooledList<T>());
						pooledList = (PooledList<T>)obj3;
					}
					((PooledList<T>)obj3).Add(target2);
				}
				else
				{
					NeedCompact = true;
				}
			}
		}
		return pooledList;
	}
}
