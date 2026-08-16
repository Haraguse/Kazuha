using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

internal class TopNavigationViewDataProvider : SplitDataSourceBase<object, NavigationViewSplitVectorID, double>
{
	private Action<NotifyCollectionChangedEventArgs> _dataChangedCallback;

	private IEnumerable _rawDataSource;

	private ItemsSourceView _dataSource;

	private double _overflowButtonCachedWidth;

	public override int Size
	{
		get
		{
			if (_dataSource != null)
			{
				return _dataSource.Count;
			}
			return 0;
		}
	}

	protected override NavigationViewSplitVectorID DefaultVectorIDOnInsert => NavigationViewSplitVectorID.NotInitialized;

	protected override double DefaultAttachedData => double.MinValue;

	public int PrimaryListSize => GetPrimaryItems().Count;

	public int NavigationViewItemCountInPrimaryList
	{
		get
		{
			int num = 0;
			for (int i = 0; i < Size; i++)
			{
				if (IsItemInPrimaryList(i) && IsContainerNavigationViewItem(i))
				{
					num++;
				}
			}
			return num;
		}
	}

	public int NavigationViewItemCountInTopNav
	{
		get
		{
			int num = 0;
			for (int i = 0; i < Size; i++)
			{
				if (IsContainerNavigationViewItem(i))
				{
					num++;
				}
			}
			return num;
		}
	}

	public double OverflowButtonWidth
	{
		get
		{
			return _overflowButtonCachedWidth;
		}
		set
		{
			_overflowButtonCachedWidth = value;
		}
	}

	public TopNavigationViewDataProvider(FANavigationView owner)
		: base(5)
	{
		Func<object, int> indexOfFunction = (object value) => IndexOf(value);
		SplitVector<object, NavigationViewSplitVectorID> splitVector = new SplitVector<object, NavigationViewSplitVectorID>(NavigationViewSplitVectorID.PrimaryList, indexOfFunction);
		SplitVector<object, NavigationViewSplitVectorID> splitVector2 = new SplitVector<object, NavigationViewSplitVectorID>(NavigationViewSplitVectorID.OverflowList, indexOfFunction);
		InitializeSplitVectors(splitVector, splitVector2);
	}

	public IList<object> GetPrimaryItems()
	{
		return GetVector(NavigationViewSplitVectorID.PrimaryList).Vector;
	}

	public IList<object> GetOverflowItems()
	{
		return GetVector(NavigationViewSplitVectorID.OverflowList).Vector;
	}

	public void SetDataSource(IEnumerable rawData)
	{
		if (ShouldChangeDataSource(rawData))
		{
			ItemsSourceView val = null;
			if (rawData != null)
			{
				val = ItemsSourceView.GetOrCreate(rawData);
			}
			ChangeDataSource(val);
			_rawDataSource = rawData;
			if (val != null)
			{
				MoveAllItemsToPrimaryList();
			}
		}
	}

	public bool ShouldChangeDataSource(IEnumerable rawData)
	{
		return rawData != _rawDataSource;
	}

	public void OnRawDataChanged(Action<NotifyCollectionChangedEventArgs> dataChangedCallback)
	{
		_dataChangedCallback = dataChangedCallback;
	}

	public override int IndexOf(object value)
	{
		if (_dataSource != null)
		{
			return _dataSource.IndexOf(value);
		}
		return -1;
	}

	public int IndexOf(object value, NavigationViewSplitVectorID id)
	{
		int num = IndexOf(value);
		int result = -1;
		if (num != -1)
		{
			SplitVector<object, NavigationViewSplitVectorID> vectorForItem = GetVectorForItem(num);
			if (vectorForItem != null && vectorForItem.GetVectorIDForItem() == id)
			{
				result = vectorForItem.IndexFromIndexInOriginalVector(num);
			}
		}
		return result;
	}

	public override object GetAt(int index)
	{
		if (_dataSource != null)
		{
			return _dataSource.GetAt(index);
		}
		return null;
	}

	public void MoveAllItemsToPrimaryList()
	{
		for (int i = 0; i < Size; i++)
		{
			MoveItemToVector(i, NavigationViewSplitVectorID.PrimaryList);
		}
	}

	public IList<int> ConvertPrimaryIndexToIndex(IList<int> indicesInPrimary)
	{
		List<int> list = new List<int>();
		if (indicesInPrimary.Count != 0)
		{
			SplitVector<object, NavigationViewSplitVectorID> vector = GetVector(NavigationViewSplitVectorID.PrimaryList);
			if (vector != null)
			{
				list.AddRange(indicesInPrimary.Select((int index) => vector.IndexToIndexInOriginalVector(index)));
			}
		}
		return list;
	}

	public int ConvertOriginalIndexToIndex(int originalIndex)
	{
		return GetVector(IsItemInPrimaryList(originalIndex) ? NavigationViewSplitVectorID.PrimaryList : NavigationViewSplitVectorID.OverflowList).IndexFromIndexInOriginalVector(originalIndex);
	}

	public void MoveItemsOutOfPrimaryList(IList<int> indices)
	{
		MoveItemsToList(indices, NavigationViewSplitVectorID.OverflowList);
	}

	public void MoveItemsToList(IList<int> indices, NavigationViewSplitVectorID id)
	{
		foreach (int index in indices)
		{
			MoveItemToVector(index, id);
		}
	}

	public void UpdateWidthForPrimaryItem(int indexInPrimary, double width)
	{
		SplitVector<object, NavigationViewSplitVectorID> vector = GetVector(NavigationViewSplitVectorID.PrimaryList);
		if (vector != null)
		{
			int index = vector.IndexToIndexInOriginalVector(indexInPrimary);
			SetWidthForItem(index, width);
		}
	}

	public double WidthRequiredToRecoveryAllItemsToPrimary()
	{
		double num = 0.0;
		for (int i = 0; i < Size; i++)
		{
			if (!IsItemInPrimaryList(i))
			{
				num += GetWidthForItem(i);
			}
		}
		num -= _overflowButtonCachedWidth;
		return Math.Max(0.0, num);
	}

	public bool HasInvalidWidth(IList<int> items)
	{
		bool result = false;
		foreach (int item in items)
		{
			if (!IsValidWidthForItem(item))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public double GetWidthForItem(int index)
	{
		double num = AttachedData(index);
		if (!IsValidWidth(num))
		{
			num = 0.0;
		}
		return num;
	}

	public double CalculateWidthForItems(IList<int> items)
	{
		double num = 0.0;
		foreach (int item in items)
		{
			num += GetWidthForItem(item);
		}
		return num;
	}

	public void InvalidWidthCache()
	{
		ResetAttachedData(-1.0);
	}

	public bool IsItemSelectableInPrimaryList(object value)
	{
		return IndexOf(value) != -1;
	}

	public void OnDataSourceChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		switch (args.Action)
		{
		case NotifyCollectionChangedAction.Add:
			OnInsertAt(args.NewStartingIndex, args.NewItems.Count);
			break;
		case NotifyCollectionChangedAction.Remove:
			OnRemoveAt(args.OldStartingIndex, args.OldItems.Count);
			break;
		case NotifyCollectionChangedAction.Reset:
			OnClear();
			break;
		case NotifyCollectionChangedAction.Replace:
			OnRemoveAt(args.OldStartingIndex, args.OldItems.Count);
			OnInsertAt(args.NewStartingIndex, args.NewItems.Count);
			break;
		}
		_dataChangedCallback?.Invoke(args);
	}

	public bool IsValidWidth(double width)
	{
		if (width >= 0.0)
		{
			return width < double.MaxValue;
		}
		return false;
	}

	public bool IsValidWidthForItem(int index)
	{
		double width = AttachedData(index);
		return IsValidWidth(width);
	}

	public void SetWidthForItem(int index, double width)
	{
		if (IsValidWidth(width))
		{
			AttachedData(index, width);
		}
	}

	public void ChangeDataSource(ItemsSourceView newValue)
	{
		ItemsSourceView dataSource = _dataSource;
		if (dataSource != newValue)
		{
			if (dataSource != null)
			{
				INotifyCollectionChanged notifyCollectionChanged = (INotifyCollectionChanged)dataSource;
				if (notifyCollectionChanged != null)
				{
					notifyCollectionChanged.CollectionChanged -= OnDataSourceChanged;
				}
			}
			Clear();
			_dataSource = newValue;
			SyncAndInitVectorFlagsWithID(NavigationViewSplitVectorID.NotInitialized, DefaultAttachedData);
			if (newValue != null)
			{
				((INotifyCollectionChanged)newValue).CollectionChanged += OnDataSourceChanged;
			}
		}
		MoveItemsToVector(NavigationViewSplitVectorID.NotInitialized);
	}

	public bool IsItemInPrimaryList(int index)
	{
		return GetVectorIDForItem(index) == NavigationViewSplitVectorID.PrimaryList;
	}

	public bool IsContainerNavigationViewItem(int index)
	{
		object at = GetAt(index);
		if (at is FANavigationViewItemHeader || at is FANavigationViewItemSeparator)
		{
			return false;
		}
		return true;
	}

	public bool IsContainerNavigationViewHeader(int index)
	{
		if (GetAt(index) is FANavigationViewItemHeader)
		{
			return true;
		}
		return false;
	}

	public void MoveItemsToPrimaryList(IList<int> indexes)
	{
		MoveItemsToList(indexes, NavigationViewSplitVectorID.PrimaryList);
	}
}
