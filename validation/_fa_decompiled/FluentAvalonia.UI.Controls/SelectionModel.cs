using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

internal class SelectionModel : INotifyPropertyChanged, IDisposable
{
	private SelectionNode _rootNode;

	private bool _singleSelect;

	private IReadOnlyList<IndexPath> _selectedIndicesCached;

	private IReadOnlyList<object> _selectedItemsCached;

	private SelectionModelChildrenRequestedEventArgs _childrenRequestedEventArgs;

	private SelectionModelSelectionChangedEventArgs _selectionChangedEventArgs;

	public object Source
	{
		get
		{
			return _rootNode.Source;
		}
		set
		{
			ClearSelection();
			_rootNode.Source = value;
			OnSelectionChanged();
			RaisePropertyChanged("Source");
		}
	}

	public bool SingleSelect
	{
		get
		{
			return _singleSelect;
		}
		set
		{
			if (_singleSelect != value)
			{
				_singleSelect = value;
				IReadOnlyList<IndexPath> selectedIndices = SelectedIndices;
				if (value && selectedIndices != null && selectedIndices.Count > 1)
				{
					IndexPath index = selectedIndices[0];
					ClearSelection();
					SelectWithPathImpl(index, select: true, raiseSelectionChanged: true);
				}
				RaisePropertyChanged("SingleSelect");
			}
		}
	}

	public IndexPath AnchorIndex
	{
		get
		{
			IndexPath result = IndexPath.Unselected;
			if (_rootNode.AnchorIndex >= 0)
			{
				List<int> list = new List<int>();
				SelectionNode selectionNode = _rootNode;
				while (selectionNode != null && selectionNode.AnchorIndex >= 0)
				{
					list.Add(selectionNode.AnchorIndex);
					selectionNode = selectionNode.GetAt(selectionNode.AnchorIndex, realizeChild: false);
				}
				result = new IndexPath(list);
			}
			return result;
		}
		set
		{
			if (value != IndexPath.Unselected)
			{
				SelectionTreeHelper.TraverseIndexPath(_rootNode, value, realizeChildren: true, delegate(SelectionNode childNode, IndexPath path, int depth, int childIndex)
				{
					childNode.AnchorIndex = path.GetAt(depth);
				});
			}
			else
			{
				_rootNode.AnchorIndex = -1;
			}
			RaisePropertyChanged("AnchorIndex");
		}
	}

	public IndexPath SelectedIndex
	{
		get
		{
			IndexPath result = IndexPath.Unselected;
			IReadOnlyList<IndexPath> selectedIndices = SelectedIndices;
			if (selectedIndices != null && selectedIndices.Count > 0)
			{
				result = selectedIndices[0];
			}
			return result;
		}
		set
		{
			bool? flag = IsSelectedAt(value);
			if (!flag.HasValue || (flag.HasValue && !flag.Value))
			{
				ClearSelection(resetAnchor: true, raiseSelectionChanged: false);
				SelectWithPathImpl(value, select: true, raiseSelectionChanged: false);
				OnSelectionChanged();
			}
		}
	}

	public object SelectedItem
	{
		get
		{
			object result = null;
			IReadOnlyList<object> selectedItems = SelectedItems;
			if (selectedItems != null && selectedItems.Count > 0)
			{
				result = selectedItems[0];
			}
			return result;
		}
	}

	public IReadOnlyList<object> SelectedItems
	{
		get
		{
			if (_selectedItemsCached == null)
			{
				List<SelectedItemInfo> selectedInfos = new List<SelectedItemInfo>();
				if (_rootNode.Source != null)
				{
					SelectionTreeHelper.Traverse(_rootNode, realizeChildren: false, delegate(SelectionTreeHelper.TreeWalkNodeInfo currentInfo)
					{
						if (currentInfo.Node.SelectedCount > 0)
						{
							selectedInfos.Add(new SelectedItemInfo(currentInfo.Node, currentInfo.Path));
						}
					});
				}
				SelectedItems<object> selectedItemsCached = new SelectedItems<object>(selectedInfos, delegate(IList<SelectedItemInfo> infos, int index)
				{
					int num = 0;
					object result = null;
					foreach (SelectedItemInfo info in infos)
					{
						if (!info.Node.TryGetTarget(out var target))
						{
							throw new InvalidOperationException("Selection has changed since SelectedItems property was read.");
						}
						int selectedCount = target.SelectedCount;
						if (index >= num && index < num + selectedCount)
						{
							int num2 = target.SelectedIndices()[index - num];
							result = target.ItemsSourceView.GetAt(num2);
							break;
						}
						num += selectedCount;
					}
					return result;
				});
				_selectedItemsCached = selectedItemsCached;
			}
			return _selectedItemsCached;
		}
	}

	public IReadOnlyList<IndexPath> SelectedIndices
	{
		get
		{
			if (_selectedIndicesCached == null)
			{
				List<SelectedItemInfo> selectedInfos = new List<SelectedItemInfo>();
				SelectionTreeHelper.Traverse(_rootNode, realizeChildren: false, delegate(SelectionTreeHelper.TreeWalkNodeInfo currentInfo)
				{
					if (currentInfo.Node.SelectedCount > 0)
					{
						selectedInfos.Add(new SelectedItemInfo(currentInfo.Node, currentInfo.Path));
					}
				});
				SelectedItems<IndexPath> selectedIndicesCached = new SelectedItems<IndexPath>(selectedInfos, delegate(IList<SelectedItemInfo> infos, int index)
				{
					int num = 0;
					IndexPath result = IndexPath.Unselected;
					foreach (SelectedItemInfo info in infos)
					{
						if (!info.Node.TryGetTarget(out var target))
						{
							throw new InvalidOperationException("Selection has changed since SelectedIndices property was read.");
						}
						int selectedCount = target.SelectedCount;
						if (index >= num && index < num + selectedCount)
						{
							int childIndex = target.SelectedIndices()[index - num];
							IndexPath path = info.Path;
							result = path.CloneWithChildIndex(childIndex);
							break;
						}
						num += selectedCount;
					}
					return result;
				});
				_selectedIndicesCached = selectedIndicesCached;
			}
			return _selectedIndicesCached;
		}
	}

	internal SelectionNode SharedLeafNode { get; private set; }

	public event EventHandler<SelectionModelChildrenRequestedEventArgs> ChildrenRequested;

	public event EventHandler<SelectionModelSelectionChangedEventArgs> SelectionChanged;

	public event PropertyChangedEventHandler PropertyChanged;

	public SelectionModel()
	{
		_rootNode = new SelectionNode(this, null);
		SharedLeafNode = new SelectionNode(this, null);
	}

	public void SetAnchorIndex(int index)
	{
		AnchorIndex = new IndexPath(index);
	}

	public void SetAnchorIndex(int groupIndex, int itemIndex)
	{
		new IndexPath(groupIndex, itemIndex);
	}

	public void Select(int index)
	{
		SelectImpl(index, select: true);
	}

	public void Select(int groupIndex, int itemIndex)
	{
		SelectWithGroupImpl(groupIndex, itemIndex, select: true);
	}

	public void SelectAt(IndexPath index)
	{
		SelectWithPathImpl(index, select: true, raiseSelectionChanged: true);
	}

	public void Deselect(int index)
	{
		SelectImpl(index, select: false);
	}

	public void Deselect(int groupIndex, int itemIndex)
	{
		SelectWithGroupImpl(groupIndex, itemIndex, select: false);
	}

	public void DeselectAt(IndexPath index)
	{
		SelectWithPathImpl(index, select: false, raiseSelectionChanged: true);
	}

	public bool? IsSelected(int index)
	{
		return _rootNode.IsSelectedWithPartial(index);
	}

	public bool? IsSelected(int groupIndex, int itemIndex)
	{
		bool? result = false;
		SelectionNode at = _rootNode.GetAt(groupIndex, realizeChild: false);
		if (at != null)
		{
			return at.IsSelectedWithPartial(itemIndex);
		}
		return result;
	}

	public bool? IsSelectedAt(IndexPath index)
	{
		IndexPath indexPath = index;
		bool flag = true;
		SelectionNode selectionNode = _rootNode;
		for (int i = 0; i < indexPath.GetSize() - 1; i++)
		{
			int at = indexPath.GetAt(i);
			selectionNode = selectionNode.GetAt(at, realizeChild: false);
			if (selectionNode == null)
			{
				flag = false;
				break;
			}
		}
		bool? result = false;
		if (flag)
		{
			int size = indexPath.GetSize();
			if (size == 0)
			{
				return SelectionNode.ConvertToNullableBool(selectionNode.EvaluateIsSelectedBasedOnChildrenNodes());
			}
			return selectionNode.IsSelectedWithPartial(indexPath.GetAt(size - 1));
		}
		return result;
	}

	public void SelectRangeFromAnchor(int index)
	{
		SelectRangeFromAnchorImpl(index, select: true);
	}

	public void SelectRangeFromAnchor(int endGroupIndex, int endItemIndex)
	{
		SelectRangeFromAnchorWithGroupImpl(endGroupIndex, endItemIndex, select: true);
	}

	public void SelectRangeFromAnchorTo(IndexPath index)
	{
		SelectRangeImpl(AnchorIndex, index, select: true);
	}

	public void DeselectRangeFromAnchor(int index)
	{
		SelectRangeFromAnchorImpl(index, select: false);
	}

	public void DeselectRangeFromAnchor(int endGroupIndex, int endItemIndex)
	{
		SelectRangeFromAnchorWithGroupImpl(endGroupIndex, endItemIndex, select: false);
	}

	public void DeselectRangeFromAnchorTo(IndexPath index)
	{
		SelectRangeImpl(AnchorIndex, index, select: false);
	}

	public void SelectRange(IndexPath start, IndexPath end)
	{
		SelectRangeImpl(start, end, select: true);
	}

	public void DeselectRange(IndexPath start, IndexPath end)
	{
		SelectRangeImpl(start, end, select: false);
	}

	public void SelectAll()
	{
		SelectionTreeHelper.Traverse(_rootNode, realizeChildren: true, delegate(SelectionTreeHelper.TreeWalkNodeInfo info)
		{
			if (info.Node.DataCount > 0)
			{
				info.Node.SelectAll();
			}
		});
		OnSelectionChanged();
	}

	public void ClearSelection()
	{
		ClearSelection(resetAnchor: true, raiseSelectionChanged: true);
	}

	public void Dispose()
	{
		ClearSelection(resetAnchor: false, raiseSelectionChanged: false);
		_rootNode = null;
		SharedLeafNode = null;
		_selectedIndicesCached = null;
		_selectedItemsCached = null;
	}

	private void OnPropertyChanged(string propertyName = null)
	{
		RaisePropertyChanged(propertyName);
	}

	private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	internal void OnSelectionInvalidatedDueToCollectionChange()
	{
		OnSelectionChanged();
	}

	internal object ResolvePath(object data, IndexPath dataIndexPath)
	{
		object result = null;
		if (ChildrenRequested != null)
		{
			if (_childrenRequestedEventArgs == null)
			{
				_childrenRequestedEventArgs = new SelectionModelChildrenRequestedEventArgs(data, dataIndexPath, throwOnAccess: false);
			}
			else
			{
				_childrenRequestedEventArgs.Initialize(data, dataIndexPath, throwOnAccess: false);
			}
			ChildrenRequested?.Invoke(this, _childrenRequestedEventArgs);
			result = _childrenRequestedEventArgs.Children;
			_childrenRequestedEventArgs.Initialize(null, IndexPath.Unselected, throwOnAccess: true);
		}
		else if (data is ItemsSourceView || data is IEnumerable)
		{
			result = data;
		}
		return result;
	}

	private void ClearSelection(bool resetAnchor, bool raiseSelectionChanged)
	{
		SelectionTreeHelper.Traverse(_rootNode, realizeChildren: false, delegate(SelectionTreeHelper.TreeWalkNodeInfo info)
		{
			info.Node.Clear();
		});
		if (resetAnchor)
		{
			AnchorIndex = IndexPath.Unselected;
		}
		if (raiseSelectionChanged)
		{
			OnSelectionChanged();
		}
	}

	private void OnSelectionChanged()
	{
		_selectedIndicesCached = null;
		_selectedItemsCached = null;
		if (SelectionChanged != null)
		{
			if (_selectionChangedEventArgs == null)
			{
				_selectionChangedEventArgs = new SelectionModelSelectionChangedEventArgs();
			}
			SelectionChanged(this, _selectionChangedEventArgs);
		}
		RaisePropertyChanged("SelectedIndex");
		RaisePropertyChanged("SelectedIndices");
		if (_rootNode.Source != null)
		{
			RaisePropertyChanged("SelectedItem");
			RaisePropertyChanged("SelectedItems");
		}
	}

	private void SelectImpl(int index, bool select)
	{
		if (_rootNode.IsSelected(index) != select)
		{
			if (_singleSelect)
			{
				ClearSelection(resetAnchor: true, raiseSelectionChanged: false);
			}
			if (_rootNode.Select(index, select))
			{
				AnchorIndex = new IndexPath(index);
			}
			OnSelectionChanged();
		}
	}

	private void SelectWithGroupImpl(int groupIndex, int itemIndex, bool select)
	{
		if (_singleSelect)
		{
			ClearSelection(resetAnchor: true, raiseSelectionChanged: false);
		}
		if (_rootNode.GetAt(groupIndex, realizeChild: true).Select(itemIndex, select))
		{
			AnchorIndex = new IndexPath(groupIndex, itemIndex);
		}
		OnSelectionChanged();
	}

	private void SelectWithPathImpl(IndexPath index, bool select, bool raiseSelectionChanged)
	{
		bool flag = true;
		if (_singleSelect)
		{
			IndexPath selectedIndex = SelectedIndex;
			if (selectedIndex != IndexPath.Unselected)
			{
				if (select && selectedIndex.CompareTo(index) == 0)
				{
					flag = false;
				}
			}
			else
			{
				flag = select;
			}
		}
		if (!flag)
		{
			return;
		}
		bool selected = false;
		bool changedSelection = false;
		if (_singleSelect & select)
		{
			ClearSelection(resetAnchor: true, raiseSelectionChanged: false);
		}
		SelectionTreeHelper.TraverseIndexPath(_rootNode, index, realizeChildren: true, delegate(SelectionNode currentNode, IndexPath path, int depth, int childIndex)
		{
			if (depth == path.GetSize() - 1)
			{
				if (currentNode.IsSelected(childIndex) != select)
				{
					changedSelection = true;
				}
				selected = currentNode.Select(childIndex, select);
			}
		});
		if (selected)
		{
			AnchorIndex = index;
		}
		_selectedIndicesCached = null;
		_selectedItemsCached = null;
		if (raiseSelectionChanged & changedSelection)
		{
			OnSelectionChanged();
		}
	}

	private void SelectRangeFromAnchorImpl(int index, bool select)
	{
		int begin = 0;
		IndexPath anchorIndex = AnchorIndex;
		if (anchorIndex != IndexPath.Unselected)
		{
			begin = anchorIndex.GetAt(0);
		}
		if (_rootNode.SelectRange(new IndexRange(begin, index), select))
		{
			OnSelectionChanged();
		}
	}

	private void SelectRangeFromAnchorWithGroupImpl(int endGroupIndex, int endItemIndex, bool select)
	{
		int num = 0;
		int num2 = 0;
		IndexPath anchorIndex = AnchorIndex;
		if (anchorIndex != IndexPath.Unselected)
		{
			num = anchorIndex.GetAt(0);
			num2 = anchorIndex.GetAt(1);
		}
		if (num > endGroupIndex || (num == endGroupIndex && num2 > endItemIndex))
		{
			int num3 = num;
			num = endGroupIndex;
			endGroupIndex = num3;
			int num4 = num2;
			num2 = endItemIndex;
			endItemIndex = num4;
		}
		bool flag = false;
		for (int i = num; i <= endGroupIndex; i++)
		{
			SelectionNode at = _rootNode.GetAt(i, realizeChild: true);
			int begin = ((i == num) ? num2 : 0);
			int end = ((i == endGroupIndex) ? endItemIndex : (at.DataCount - 1));
			flag |= at.SelectRange(new IndexRange(begin, end), select);
		}
		if (flag)
		{
			OnSelectionChanged();
		}
	}

	private void SelectRangeImpl(IndexPath start, IndexPath end, bool select)
	{
		IndexPath indexPath = start;
		IndexPath indexPath2 = end;
		if (indexPath2.CompareTo(indexPath) == -1)
		{
			IndexPath indexPath3 = indexPath;
			indexPath = indexPath2;
			indexPath2 = indexPath3;
		}
		SelectionTreeHelper.TraverseRangeRealizeChildren(_rootNode, indexPath, indexPath2, delegate(SelectionTreeHelper.TreeWalkNodeInfo info)
		{
			if (info.Node.DataCount == 0)
			{
				info.ParentNode.Select(info.Path.GetAt(info.Path.GetSize() - 1), select);
			}
		});
		OnSelectionChanged();
	}
}
