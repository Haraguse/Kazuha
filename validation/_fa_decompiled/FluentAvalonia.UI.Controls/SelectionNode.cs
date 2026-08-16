using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Tracks nested selection.
/// </summary>
/// <remarks>
/// SelectionNode is the internal tree data structure that we keep track of for selection in 
/// a nested scenario. This would map to one ItemsSourceView/Collection. This node reacts to
/// collection changes and keeps the selected indices up to date. This can either be a leaf
/// node or a non leaf node.
/// </remarks>
internal class SelectionNode : IDisposable
{
	private readonly SelectionModel _manager;

	private readonly List<SelectionNode> _childrenNodes = new List<SelectionNode>();

	private readonly SelectionNode _parent;

	private readonly List<IndexRange> _selected = new List<IndexRange>();

	private object _source;

	private ItemsSourceView _dataSource;

	private int _selectedCount;

	private readonly List<int> _selectedIndicesCached = new List<int>();

	private bool _selectedIndicesCacheIsValid;

	private int _anchorIndex = -1;

	private int _realizedChildrenNodeCount;

	public object Source
	{
		get
		{
			return _source;
		}
		set
		{
			if (_source != value)
			{
				ClearSelection();
				UnhookCollectionChangedHandler();
				_source = value;
				ItemsSourceView val = (ItemsSourceView)((value is ItemsSourceView) ? value : null);
				if (value != null && val == null)
				{
					val = ItemsSourceView.GetOrCreate(value as IEnumerable);
				}
				_dataSource = val;
				HookupCollectionChangedHandler();
				OnSelectionChanged();
			}
		}
	}

	public ItemsSourceView ItemsSourceView => _dataSource;

	public int DataCount
	{
		get
		{
			if (_dataSource == null)
			{
				return 0;
			}
			return _dataSource.Count;
		}
	}

	public int ChildrenNodeCount => _childrenNodes.Count;

	public int RealizedChildrenNodeCount => _realizedChildrenNodeCount;

	public int AnchorIndex
	{
		get
		{
			return _anchorIndex;
		}
		set
		{
			_anchorIndex = value;
		}
	}

	public IndexPath IndexPath
	{
		get
		{
			List<int> list = new List<int>();
			SelectionNode parent = _parent;
			SelectionNode item = this;
			while (parent != null)
			{
				int item2 = parent._childrenNodes.IndexOf(item);
				list.Insert(0, item2);
				item = parent;
				parent = parent._parent;
			}
			return new IndexPath(list);
		}
	}

	public int SelectedCount => _selectedCount;

	public SelectionNode(SelectionModel manager, SelectionNode parent)
	{
		_manager = manager;
		_parent = parent;
	}

	public SelectionNode GetAt(int index, bool realizeChild)
	{
		SelectionNode selectionNode = null;
		if (realizeChild)
		{
			if (_childrenNodes.Count == 0 && _dataSource != null)
			{
				for (int i = 0; i < _dataSource.Count; i++)
				{
					_childrenNodes.Add(null);
				}
			}
			if (_childrenNodes[index] == null)
			{
				object at = _dataSource.GetAt(index);
				if (at != null)
				{
					IndexPath dataIndexPath = IndexPath.CloneWithChildIndex(index);
					object obj = _manager.ResolvePath(at, dataIndexPath);
					if (obj != null)
					{
						selectionNode = new SelectionNode(_manager, this);
						selectionNode.Source = obj;
					}
					else
					{
						selectionNode = _manager.SharedLeafNode;
					}
				}
				else
				{
					selectionNode = _manager.SharedLeafNode;
				}
				_childrenNodes[index] = selectionNode;
				_realizedChildrenNodeCount++;
			}
			else
			{
				selectionNode = _childrenNodes[index];
			}
		}
		else if (_childrenNodes.Count > 0)
		{
			selectionNode = _childrenNodes[index];
		}
		return selectionNode;
	}

	public bool IsSelected(int index)
	{
		bool result = false;
		for (int i = 0; i < _selected.Count; i++)
		{
			if (_selected[i].Contains(index))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public bool? IsSelectedWithPartial()
	{
		bool? result = false;
		if (_parent != null)
		{
			int num = _parent._childrenNodes.IndexOf(this);
			if (num != -1)
			{
				return _parent.IsSelectedWithPartial(num);
			}
		}
		return result;
	}

	public bool? IsSelectedWithPartial(int index)
	{
		SelectionState selectionState = SelectionState.NotSelected;
		selectionState = ((_childrenNodes.Count != 0 && _childrenNodes.Count > index && _childrenNodes[index] != null && _childrenNodes[index] != _manager.SharedLeafNode) ? _childrenNodes[index].EvaluateIsSelectedBasedOnChildrenNodes() : ((!IsSelected(index)) ? SelectionState.NotSelected : SelectionState.Selected));
		return ConvertToNullableBool(selectionState);
	}

	public int SelectedIndex()
	{
		if (SelectedCount <= 0)
		{
			return -1;
		}
		return SelectedIndices()[0];
	}

	public void SelectedIndex(int value)
	{
		if (IsValidIndex(value) && (SelectedCount != -1 || !IsSelected(value)))
		{
			ClearSelection();
			if (value != -1)
			{
				Select(value, select: true);
			}
		}
	}

	public List<int> SelectedIndices()
	{
		if (!_selectedIndicesCacheIsValid)
		{
			_selectedIndicesCacheIsValid = true;
			foreach (IndexRange item in _selected)
			{
				for (int i = item.Begin; i <= item.End; i++)
				{
					if (!_selectedIndicesCached.Contains(i))
					{
						_selectedIndicesCached.Add(i);
					}
				}
			}
			_selectedIndicesCached.Sort();
		}
		return _selectedIndicesCached;
	}

	public bool Select(int index, bool select)
	{
		return Select(index, select, raiseOnSelectionChanged: true);
	}

	public bool ToggleSelect(int index)
	{
		return Select(index, !IsSelected(index));
	}

	public void SelectAll()
	{
		if (_dataSource != null)
		{
			int count = _dataSource.Count;
			if (count > 0)
			{
				SelectRange(new IndexRange(0, count - 1), select: true);
			}
		}
	}

	public void Clear()
	{
		ClearSelection();
	}

	public bool SelectRange(IndexRange range, bool select)
	{
		if (IsValidIndex(range.Begin) && IsValidIndex(range.End))
		{
			if (select)
			{
				AddRange(range, raiseOnSelectionChanged: true);
			}
			else
			{
				RemoveRange(range, raiseOnSelectionChanged: true);
			}
			return true;
		}
		return false;
	}

	public void Dispose()
	{
		UnhookCollectionChangedHandler();
	}

	private void HookupCollectionChangedHandler()
	{
		if (_dataSource != null)
		{
			_dataSource.CollectionChanged += OnSourceListChanged;
		}
	}

	private void UnhookCollectionChangedHandler()
	{
		if (_dataSource != null)
		{
			_dataSource.CollectionChanged -= OnSourceListChanged;
		}
	}

	private bool IsValidIndex(int index)
	{
		if (ItemsSourceView != null)
		{
			if (index >= 0)
			{
				return index <= ItemsSourceView.Count;
			}
			return false;
		}
		return true;
	}

	private void AddRange(IndexRange addRange, bool raiseOnSelectionChanged)
	{
		int selectedCount = SelectedCount;
		for (int i = addRange.Begin; i <= addRange.End; i++)
		{
			if (!IsSelected(i))
			{
				_selectedCount++;
			}
		}
		if (selectedCount != _selectedCount)
		{
			_selected.Add(addRange);
			if (raiseOnSelectionChanged)
			{
				OnSelectionChanged();
			}
		}
	}

	private void RemoveRange(IndexRange removeRange, bool raiseOnSelectionChanged)
	{
		int selectedCount = _selectedCount;
		for (int i = removeRange.Begin; i <= removeRange.End; i++)
		{
			if (IsSelected(i))
			{
				_selectedCount--;
			}
		}
		if (selectedCount == _selectedCount)
		{
			return;
		}
		List<IndexRange> list = new List<IndexRange>();
		List<IndexRange> list2 = new List<IndexRange>();
		foreach (IndexRange item in _selected)
		{
			if (removeRange.Intersects(item))
			{
				IndexRange before = new IndexRange(-1, -1);
				IndexRange after = new IndexRange(-1, -1);
				IndexRange after2 = new IndexRange(-1, -1);
				if (item.Contains(removeRange.Begin - 1))
				{
					item.Split(removeRange.Begin - 1, out before, out after);
					list2.Add(before);
				}
				if (item.Contains(removeRange.End) && item.Split(removeRange.End, out after, out after2))
				{
					list2.Add(after2);
				}
				list.Add(item);
			}
		}
		if (list.Count <= 0 && list2.Count <= 0)
		{
			return;
		}
		foreach (IndexRange item2 in list)
		{
			int index = _selected.IndexOf(item2);
			_selected.RemoveAt(index);
		}
		foreach (IndexRange item3 in list2)
		{
			_selected.Add(item3);
		}
		if (raiseOnSelectionChanged)
		{
			OnSelectionChanged();
		}
	}

	private void ClearSelection()
	{
		if (_selected.Count > 0)
		{
			_selected.Clear();
			OnSelectionChanged();
		}
		_selectedCount = 0;
		AnchorIndex = -1;
		foreach (SelectionNode childrenNode in _childrenNodes)
		{
			childrenNode?.Dispose();
		}
		_childrenNodes.Clear();
	}

	private bool Select(int index, bool select, bool raiseOnSelectionChanged)
	{
		if (IsValidIndex(index))
		{
			if (IsSelected(index) == select)
			{
				return true;
			}
			IndexRange indexRange = new IndexRange(index, index);
			if (select)
			{
				AddRange(indexRange, raiseOnSelectionChanged);
			}
			else
			{
				RemoveRange(indexRange, raiseOnSelectionChanged);
			}
			return true;
		}
		return false;
	}

	private void OnSourceListChanged(object dataSource, NotifyCollectionChangedEventArgs args)
	{
		bool flag = false;
		switch (args.Action)
		{
		case NotifyCollectionChangedAction.Add:
			flag = OnItemsAdded(args.NewStartingIndex, args.NewItems.Count);
			break;
		case NotifyCollectionChangedAction.Remove:
			flag = OnItemsRemoved(args.OldStartingIndex, args.OldItems.Count);
			break;
		case NotifyCollectionChangedAction.Reset:
			ClearSelection();
			flag = true;
			break;
		case NotifyCollectionChangedAction.Replace:
			flag = OnItemsRemoved(args.OldStartingIndex, args.OldItems.Count);
			flag |= OnItemsAdded(args.NewStartingIndex, args.NewItems.Count);
			break;
		}
		if (flag)
		{
			OnSelectionChanged();
			_manager.OnSelectionInvalidatedDueToCollectionChange();
		}
	}

	private bool OnItemsAdded(int index, int count)
	{
		bool flag = false;
		List<IndexRange> list = new List<IndexRange>();
		for (int i = 0; i < _selected.Count; i++)
		{
			IndexRange indexRange = _selected[i];
			if (indexRange.End >= index)
			{
				int num = indexRange.Begin;
				if (indexRange.Contains(index - 1))
				{
					IndexRange before = new IndexRange(-1, -1);
					IndexRange after = new IndexRange(-1, -1);
					indexRange.Split(index - 1, out before, out after);
					list.Add(before);
					num = index;
				}
				_selected[i] = new IndexRange(num + count, indexRange.End + count);
				flag = true;
			}
		}
		if (list.Count > 0)
		{
			_selected.AddRange(list);
		}
		if (_childrenNodes.Count > 0)
		{
			flag = true;
			for (int j = 0; j < count; j++)
			{
				_childrenNodes.Insert(index, null);
			}
		}
		if (AnchorIndex >= index)
		{
			AnchorIndex += count;
		}
		if (!flag)
		{
			for (SelectionNode parent = _parent; parent != null; parent = parent._parent)
			{
				bool? flag2 = parent.IsSelectedWithPartial();
				if (flag2.HasValue && flag2.Value)
				{
					flag = true;
					break;
				}
			}
		}
		return flag;
	}

	private bool OnItemsRemoved(int index, int count)
	{
		bool flag = false;
		if (ItemsSourceView.Count > 0)
		{
			bool flag2 = false;
			for (int i = index; i <= index + count - 1; i++)
			{
				if (IsSelected(i))
				{
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				RemoveRange(new IndexRange(index, index + count - 1), raiseOnSelectionChanged: false);
				flag = true;
			}
			for (int j = 0; j < _selected.Count; j++)
			{
				IndexRange indexRange = _selected[j];
				if (indexRange.End > index)
				{
					_selected[j] = new IndexRange(indexRange.Begin - count, indexRange.End - count);
					flag = true;
				}
			}
			if (_childrenNodes.Count > 0)
			{
				flag = true;
				for (int k = 0; k < count; k++)
				{
					if (_childrenNodes[index] != null)
					{
						_realizedChildrenNodeCount--;
					}
					_childrenNodes.RemoveAt(index);
				}
			}
			if (AnchorIndex >= index)
			{
				AnchorIndex -= count;
			}
		}
		else
		{
			ClearSelection();
			_realizedChildrenNodeCount = 0;
			flag = true;
		}
		if (!flag)
		{
			for (SelectionNode parent = _parent; parent != null; parent = parent._parent)
			{
				if (!parent.IsSelectedWithPartial().HasValue)
				{
					flag = true;
					break;
				}
			}
		}
		return flag;
	}

	private void OnSelectionChanged()
	{
		_selectedIndicesCacheIsValid = false;
		_selectedIndicesCached.Clear();
	}

	internal static bool? ConvertToNullableBool(SelectionState isSelected)
	{
		return isSelected switch
		{
			SelectionState.NotSelected => false, 
			SelectionState.Selected => true, 
			_ => null, 
		};
	}

	internal SelectionState EvaluateIsSelectedBasedOnChildrenNodes()
	{
		SelectionState selectionState = SelectionState.NotSelected;
		int realizedChildrenNodeCount = RealizedChildrenNodeCount;
		int selectedCount = SelectedCount;
		if (realizedChildrenNodeCount != 0 || selectedCount != 0)
		{
			int dataCount = DataCount;
			if (realizedChildrenNodeCount == 0 && selectedCount > 0)
			{
				selectionState = ((dataCount != selectedCount) ? SelectionState.PartiallySelected : ((dataCount != selectedCount) ? SelectionState.NotSelected : SelectionState.Selected));
			}
			else
			{
				selectedCount = 0;
				int num = 0;
				for (int i = 0; i < ChildrenNodeCount; i++)
				{
					if (GetAt(i, realizeChild: false) != null)
					{
						bool? flag = IsSelectedWithPartial(i);
						if (!flag.HasValue)
						{
							selectionState = SelectionState.PartiallySelected;
							break;
						}
						if (flag.HasValue && flag.Value)
						{
							selectedCount++;
						}
						else
						{
							num++;
						}
					}
					else if (IsSelected(i))
					{
						selectedCount++;
					}
					else
					{
						num++;
					}
					if (selectedCount > 0 && num > 0)
					{
						selectionState = SelectionState.PartiallySelected;
						break;
					}
				}
				if (selectionState != SelectionState.PartiallySelected)
				{
					selectionState = ((selectedCount == 0 || selectedCount == dataCount) ? ((selectedCount != dataCount) ? SelectionState.NotSelected : SelectionState.Selected) : SelectionState.PartiallySelected);
				}
			}
		}
		return selectionState;
	}
}
