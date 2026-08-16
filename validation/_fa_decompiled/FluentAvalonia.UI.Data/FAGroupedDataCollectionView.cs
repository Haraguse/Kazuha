using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Logging;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Data;

public sealed class FAGroupedDataCollectionView : IFACollectionView, IEnumerable<object>, IEnumerable, IList<object>, ICollection<object>, INotifyCollectionChanged, IFAAdvancedCollectionView, INotifyPropertyChanged, IList, ICollection
{
	private struct GroupEnumerator(FAGroupedDataCollectionView owner) : IEnumerator, IEnumerator<object>, IDisposable
	{
		private int _curPos = -1;

		private int _lastGroupIndex = -1;

		private FAGroupedDataCollectionView _owner = owner;

		public object Current { get; private set; } = null;

		public bool MoveNext()
		{
			if (_owner.CollectionGroups.Count == 0)
			{
				return false;
			}
			if (_lastGroupIndex == -1)
			{
				_lastGroupIndex = 0;
			}
			IFACollectionViewGroup iFACollectionViewGroup = _owner.CollectionGroups[_lastGroupIndex];
			_curPos++;
			if (_curPos == iFACollectionViewGroup.GroupItems.Count && _lastGroupIndex == _owner.CollectionGroups.Count - 1)
			{
				Current = null;
				return false;
			}
			if (_curPos == iFACollectionViewGroup.GroupItems.Count)
			{
				_curPos = 0;
				_lastGroupIndex++;
				iFACollectionViewGroup = _owner.CollectionGroups[_lastGroupIndex];
			}
			Current = iFACollectionViewGroup.GroupItems[_curPos];
			return true;
		}

		public void Reset()
		{
			_lastGroupIndex = -1;
			_curPos = -1;
			Current = null;
		}

		public void Dispose()
		{
		}
	}

	private IEnumerable _source;

	private BindingBase _itemsBinding;

	private int _count;

	private static BindingEvaluator<object> _helper;

	private bool _hasSortOrFilter;

	private Predicate<object> _filter;

	private HashSet<string> _filterProperties;

	private IList<FASortDescription> _sortDescriptions;

	private bool _ignoreGroupChanges;

	public IAvaloniaList<IFACollectionViewGroup> CollectionGroups { get; private set; }

	public int CurrentPosition { get; private set; }

	public bool IsCurrentAfterLast => CurrentPosition >= Count;

	public bool IsCurrentBeforeFirst => CurrentPosition < 0;

	public int Count => _count;

	public bool IsReadOnly
	{
		get
		{
			if (_source is IList list)
			{
				return list.IsReadOnly;
			}
			return false;
		}
	}

	public object CurrentItem => GetItemAtIndex(CurrentPosition);

	public object this[int index]
	{
		get
		{
			return GetItemAtIndex(index);
		}
		set
		{
			ThrowICollectionViewNotMutableWhenGrouping();
		}
	}

	public bool IsLiveShapingEnabled { get; }

	public Predicate<object> Filter
	{
		get
		{
			return _filter;
		}
		set
		{
			_filter = value;
			HandleFilterChanged();
		}
	}

	public IList<FASortDescription> SortDescriptions
	{
		get
		{
			if (_sortDescriptions == null)
			{
				AvaloniaList<FASortDescription> val = new AvaloniaList<FASortDescription>();
				val.CollectionChanged += OnSortDescriptionsChanged;
				_sortDescriptions = (IList<FASortDescription>)val;
			}
			return _sortDescriptions;
		}
	}

	internal int DeferCounter { get; private set; }

	internal IEnumerable Source => _source;

	internal BindingBase ItemsBinding => _itemsBinding;

	bool IList.IsFixedSize
	{
		get
		{
			if (_source is IList list)
			{
				return list.IsFixedSize;
			}
			return false;
		}
	}

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => null;

	int ICollection.Count => _count;

	bool IFACollectionView.HasMoreItems => false;

	public event EventHandler<object> CurrentChanged;

	public event FACurrentChangingEventHandler CurrentChanging;

	public event NotifyCollectionChangedEventHandler CollectionChanged;

	public event PropertyChangedEventHandler PropertyChanged;

	public FAGroupedDataCollectionView(IEnumerable collection, BindingBase itemsBinding = null)
		: this(collection, itemsBinding, isLiveShaping: false, null, null, null)
	{
	}

	public FAGroupedDataCollectionView(IEnumerable collection, BindingBase itemsBinding, bool isLiveShaping)
		: this(collection, itemsBinding, isLiveShaping, null, null, null)
	{
	}

	public FAGroupedDataCollectionView(IEnumerable collection, BindingBase itemsBinding, Predicate<object> filter)
		: this(collection, itemsBinding, isLiveShaping: false, filter, null, null)
	{
	}

	public FAGroupedDataCollectionView(IEnumerable collection, BindingBase itemsBinding, Predicate<object> filter, IList<string> filterProperties)
		: this(collection, itemsBinding, isLiveShaping: true, filter, filterProperties, null)
	{
	}

	public FAGroupedDataCollectionView(IEnumerable collection, BindingBase itemsBinding, IList<FASortDescription> sortDescriptions)
		: this(collection, itemsBinding, isLiveShaping: false, null, null, sortDescriptions)
	{
	}

	public FAGroupedDataCollectionView(IEnumerable collection, BindingBase itemsBinding, bool isLiveShaping, Predicate<object> filter, IList<string> filterProperties, IList<FASortDescription> sortDescriptions)
	{
		collection = collection ?? throw new ArgumentNullException("collection");
		_source = collection;
		_itemsBinding = itemsBinding;
		_hasSortOrFilter = isLiveShaping || filter != null || sortDescriptions != null;
		if (_hasSortOrFilter)
		{
			IsLiveShapingEnabled = isLiveShaping;
			_filter = filter;
			if (isLiveShaping)
			{
				_filterProperties = ((filterProperties != null) ? new HashSet<string>(filterProperties) : new HashSet<string>());
			}
			if (sortDescriptions != null)
			{
				AvaloniaList<FASortDescription> val = new AvaloniaList<FASortDescription>((IEnumerable<FASortDescription>)sortDescriptions);
				val.CollectionChanged += OnSortDescriptionsChanged;
				_sortDescriptions = (IList<FASortDescription>)val;
			}
		}
		CreateGroups();
		if (collection is INotifyCollectionChanged notifyCollectionChanged)
		{
			notifyCollectionChanged.CollectionChanged += OnBackingCollectionChanged;
		}
	}

	public bool MoveCurrentTo(object item)
	{
		return MoveCurrentToPosition(IndexOf(item));
	}

	public bool MoveCurrentToFirst()
	{
		return MoveCurrentToPosition((Count <= 0) ? (-1) : 0);
	}

	public bool MoveCurrentToLast()
	{
		return MoveCurrentToPosition((Count > 0) ? (Count - 1) : (-1));
	}

	public bool MoveCurrentToNext()
	{
		return MoveCurrentToPosition(CurrentPosition + 1);
	}

	public bool MoveCurrentToPrevious()
	{
		return MoveCurrentToPosition(CurrentPosition - 1);
	}

	public bool MoveCurrentToPosition(int pos)
	{
		if (pos == CurrentPosition)
		{
			return true;
		}
		if (pos < 0 || pos >= Count)
		{
			return false;
		}
		FACurrentChangingEventArgs e = new FACurrentChangingEventArgs();
		CurrentChanging?.Invoke(this, e);
		if (e.Cancel)
		{
			return false;
		}
		CurrentPosition = pos;
		CurrentChanged?.Invoke(this, null);
		return true;
	}

	public int IndexOf(object item)
	{
		int num = 0;
		for (int i = 0; i < CollectionGroups.Count; i++)
		{
			int num2 = CollectionGroups[i].GroupItems.IndexOf(item);
			if (num2 != -1)
			{
				return num + num2;
			}
			num += CollectionGroups[i].GroupItems.Count;
		}
		return -1;
	}

	public bool Contains(object item)
	{
		return IndexOf(item) != -1;
	}

	public void CopyTo(object[] array, int arrayIndex)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			IEnumerator<object> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				array[arrayIndex++] = enumerator.Current;
			}
		}
		catch (Exception ex)
		{
			ParametrizedLogger? val = Logger.TryGet((LogEventLevel)4, "CollectionView");
			if (val.HasValue)
			{
				ParametrizedLogger valueOrDefault = val.GetValueOrDefault();
				((ParametrizedLogger)(ref valueOrDefault)).Log<Exception>((object)"CollectionView", "Unable to copy source collection to array", ex);
			}
		}
	}

	public IEnumerator<object> GetEnumerator()
	{
		return new GroupEnumerator(this);
	}

	public IDisposable DeferRefresh()
	{
		DeferCounter++;
		return new RefreshDeferer(ReleaseDefer, CurrentItem);
	}

	internal void UpdateViewFromCollectionViewSource(Predicate<object> filter, IList<string> filterProperties, IList<FASortDescription> sortDescriptions)
	{
		using (DeferRefresh())
		{
			if (filterProperties != null)
			{
				_filterProperties.Clear();
				foreach (string filterProperty in filterProperties)
				{
					AddFilterProperty(filterProperty);
				}
			}
			Filter = filter;
			_sortDescriptions?.Clear();
			if (sortDescriptions == null)
			{
				return;
			}
			foreach (FASortDescription sortDescription in sortDescriptions)
			{
				SortDescriptions.Add(sortDescription);
			}
		}
	}

	private void ReleaseDefer(object lastCurrentItem)
	{
		DeferCounter--;
		if (DeferCounter == 0)
		{
			MoveCurrentTo(lastCurrentItem);
			Refresh();
		}
	}

	private object GetItemAtIndex(int index)
	{
		if (index == -1)
		{
			return null;
		}
		int num = 0;
		for (int i = 0; i < CollectionGroups.Count; i++)
		{
			IFACollectionViewGroup iFACollectionViewGroup = CollectionGroups[i];
			int num2 = num + iFACollectionViewGroup.GroupItems.Count;
			if (index < num2)
			{
				return iFACollectionViewGroup.GroupItems[index - num];
			}
			num = num2;
		}
		return null;
	}

	private void CreateGroups()
	{
		bool hasSortOrFilter = _hasSortOrFilter;
		List<CollectionViewGroup> list = new List<CollectionViewGroup>();
		_ignoreGroupChanges = true;
		IEnumerator enumerator = _source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			CollectionViewGroup collectionViewGroup = (hasSortOrFilter ? new SpecializedCollectionViewGroup(this, enumerator.Current, _itemsBinding != null) : new CollectionViewGroup(this, enumerator.Current, _itemsBinding != null));
			list.Add(collectionViewGroup);
			_count += collectionViewGroup.GroupItems.Count;
		}
		_ignoreGroupChanges = false;
		if (CollectionGroups == null)
		{
			CollectionGroups = (IAvaloniaList<IFACollectionViewGroup>)(object)new AvaloniaList<IFACollectionViewGroup>((IEnumerable<IFACollectionViewGroup>)list);
			return;
		}
		((ICollection<IFACollectionViewGroup>)CollectionGroups).Clear();
		CollectionGroups.AddRange((IEnumerable<IFACollectionViewGroup>)list);
	}

	private void OnBackingCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		bool hasSortOrFilter = _hasSortOrFilter;
		IAvaloniaList<IFACollectionViewGroup> collectionGroups = CollectionGroups;
		int num = 0;
		switch (args.Action)
		{
		case NotifyCollectionChangedAction.Add:
		{
			int startingIndex = GetItemCountToIndex((IList<IFACollectionViewGroup>)collectionGroups, args.NewStartingIndex);
			List<CollectionViewGroup> list2 = new List<CollectionViewGroup>(args.NewItems.Count);
			for (int i = 0; i < args.NewItems.Count; i++)
			{
				CollectionViewGroup collectionViewGroup = (hasSortOrFilter ? new SpecializedCollectionViewGroup(this, args.NewItems[i], _itemsBinding != null) : new CollectionViewGroup(this, args.NewItems[i], _itemsBinding != null));
				num += collectionViewGroup.GroupItems.Count;
				list2.Add(collectionViewGroup);
			}
			collectionGroups.InsertRange(args.NewStartingIndex, (IEnumerable<IFACollectionViewGroup>)list2);
			if (num != 0)
			{
				_count += num;
				IList<object> list3 = PopulateINCCList(args.NewStartingIndex, args.NewItems.Count, num);
				OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)list3, startingIndex));
			}
			break;
		}
		case NotifyCollectionChangedAction.Remove:
		{
			int startingIndex2 = GetItemCountToIndex((IList<IFACollectionViewGroup>)CollectionGroups, args.OldStartingIndex);
			num = GetItemCount(args.OldStartingIndex, args.OldItems.Count);
			IList<object> list4 = PopulateINCCList(args.OldStartingIndex, args.OldItems.Count, num);
			collectionGroups.RemoveRange(args.OldStartingIndex, args.OldItems.Count);
			if (num > 0)
			{
				_count -= num;
				OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)list4, startingIndex2));
			}
			if (list4 is IDisposable disposable)
			{
				disposable.Dispose();
			}
			break;
		}
		case NotifyCollectionChangedAction.Replace:
		{
			int startingIndex3 = GetItemCountToIndex((IList<IFACollectionViewGroup>)collectionGroups, args.NewStartingIndex);
			num = GetItemCount(args.OldStartingIndex, args.OldItems.Count);
			IList<object> list5 = PopulateINCCList(args.OldStartingIndex, args.NewItems.Count, num);
			_count -= num;
			List<CollectionViewGroup> list6 = new List<CollectionViewGroup>(args.NewItems.Count);
			num = 0;
			for (int j = 0; j < args.NewItems.Count; j++)
			{
				CollectionViewGroup collectionViewGroup2 = (hasSortOrFilter ? new SpecializedCollectionViewGroup(this, args.NewItems[j], _itemsBinding != null) : new CollectionViewGroup(this, args.NewItems[j], _itemsBinding != null));
				num += collectionViewGroup2.GroupItems.Count;
				list6.Add(collectionViewGroup2);
			}
			_count += num;
			CollectionGroups.InsertRange(args.NewStartingIndex, (IEnumerable<IFACollectionViewGroup>)list6);
			IList<object> list7 = null;
			if (num > 0)
			{
				list7 = PopulateINCCList(args.NewStartingIndex, args.NewItems.Count, num);
				OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, (IList)list7, (IList)list5, startingIndex3));
			}
			break;
		}
		case NotifyCollectionChangedAction.Reset:
			_count = 0;
			((ICollection<IFACollectionViewGroup>)CollectionGroups).Clear();
			CreateGroups();
			OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			break;
		case NotifyCollectionChangedAction.Move:
		{
			int oldIndex = GetItemCountToIndex((IList<IFACollectionViewGroup>)CollectionGroups, args.OldStartingIndex);
			num = GetItemCount(args.OldStartingIndex, args.OldItems.Count);
			IList<object> list = PopulateINCCList(args.OldStartingIndex, args.OldItems.Count, num);
			if (args.OldItems.Count == 1)
			{
				CollectionGroups.Move(args.OldStartingIndex, args.NewStartingIndex);
			}
			else
			{
				CollectionGroups.MoveRange(args.OldStartingIndex, args.OldItems.Count, args.NewStartingIndex);
			}
			int index = GetItemCountToIndex((IList<IFACollectionViewGroup>)CollectionGroups, args.NewStartingIndex);
			OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, (IList)list, index, oldIndex));
			break;
		}
		}
		int GetItemCount(int start, int count)
		{
			int num2 = 0;
			for (int k = start; k < start + count; k++)
			{
				num2 += CollectionGroups[k].GroupItems.Count;
			}
			return num2;
		}
		static int GetItemCountToIndex(IList<IFACollectionViewGroup> groups, int num3)
		{
			int num2 = 0;
			for (int k = 0; k < groups.Count && k != num3; k++)
			{
				num2 += groups[k].GroupItems?.Count ?? 0;
			}
			return num2;
		}
		IList<object> PopulateINCCList(int groupStart, int groupCount, int itemCount)
		{
			List<object> list8 = new List<object>(itemCount);
			for (int k = groupStart; k < groupStart + groupCount; k++)
			{
				IFACollectionViewGroup iFACollectionViewGroup = CollectionGroups[k];
				if (iFACollectionViewGroup.GroupItems.Count != 0)
				{
					list8.AddRange(iFACollectionViewGroup.GroupItems);
				}
			}
			return list8;
		}
	}

	internal IEnumerable GetItemsFromGroup(object group)
	{
		if (_helper == null)
		{
			_helper = new BindingEvaluator<object>();
		}
		_helper.UpdateBinding(_itemsBinding);
		IEnumerable obj = _helper.Evaluate(group) as IEnumerable;
		_helper.ClearDataContext();
		if (obj == null)
		{
			throw new ArgumentException($"Unable to resolve items from group of type {group.GetType()}");
		}
		return obj;
	}

	internal void GroupItemsChanged(IFACollectionViewGroup sender, NotifyCollectionChangedEventArgs args)
	{
		if (_ignoreGroupChanges)
		{
			return;
		}
		NotifyCollectionChangedEventArgs e = null;
		switch (args.Action)
		{
		case NotifyCollectionChangedAction.Add:
			_count += args.NewItems.Count;
			e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, args.NewItems, TranslateGroupIndexToFlattenedIndex(args.NewStartingIndex));
			break;
		case NotifyCollectionChangedAction.Remove:
			_count -= args.OldItems.Count;
			e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, args.OldItems, TranslateGroupIndexToFlattenedIndex(args.OldStartingIndex));
			break;
		case NotifyCollectionChangedAction.Reset:
		{
			if (sender is CollectionViewGroup collectionViewGroup)
			{
				collectionViewGroup.UpdateGroup(sender.Group);
			}
			int num = 0;
			for (int i = 0; i < CollectionGroups.Count; i++)
			{
				num += CollectionGroups[i].GroupItems.Count;
			}
			_count = num;
			e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
			break;
		}
		case NotifyCollectionChangedAction.Move:
		{
			int oldIndex = TranslateGroupIndexToFlattenedIndex(args.OldStartingIndex);
			int index = TranslateGroupIndexToFlattenedIndex(args.NewStartingIndex);
			e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, args.NewItems, index, oldIndex);
			break;
		}
		case NotifyCollectionChangedAction.Replace:
		{
			int startingIndex = TranslateGroupIndexToFlattenedIndex(args.NewStartingIndex);
			e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, args.NewItems, args.OldItems, startingIndex);
			break;
		}
		}
		CollectionChanged?.Invoke(sender, e);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
		int TranslateGroupIndexToFlattenedIndex(int num4)
		{
			int num2 = ((IList<IFACollectionViewGroup>)CollectionGroups).IndexOf(sender);
			switch (num2)
			{
			case -1:
				throw new ArgumentException("Invalid group index");
			case 0:
				return num4;
			default:
			{
				int num3 = 0;
				for (int j = 0; j < num2; j++)
				{
					num3 += CollectionGroups[j].GroupItems?.Count ?? 0;
				}
				return num3 + num4;
			}
			}
		}
	}

	internal IList<FASortDescription> GetSortDescriptions()
	{
		return _sortDescriptions;
	}

	internal HashSet<string> GetFilterProperties()
	{
		return _filterProperties;
	}

	public void Refresh()
	{
		object currentItem = CurrentItem;
		IAvaloniaList<IFACollectionViewGroup> collectionGroups = CollectionGroups;
		int num = 0;
		foreach (IFACollectionViewGroup item in (IEnumerable<IFACollectionViewGroup>)collectionGroups)
		{
			num += (item as SpecializedCollectionViewGroup).Refresh();
		}
		_count = num;
		OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		MoveCurrentTo(currentItem);
	}

	public void RefreshFilter()
	{
		HandleFilterChanged();
	}

	public void RefreshSorting()
	{
		HandleSortChanged();
	}

	public void AddFilterProperty(string propertyName)
	{
		if (IsLiveShapingEnabled)
		{
			_filterProperties.Add(propertyName);
		}
	}

	public void RemoveFilterProperty(string propertyName)
	{
		if (IsLiveShapingEnabled)
		{
			_filterProperties.Remove(propertyName);
		}
	}

	public void ClearFilterProperties()
	{
		if (IsLiveShapingEnabled)
		{
			_filterProperties.Clear();
		}
	}

	private void OnSortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (DeferCounter <= 0)
		{
			HandleSortChanged();
		}
	}

	private void HandleSortChanged()
	{
		try
		{
			_ignoreGroupChanges = true;
			foreach (IFACollectionViewGroup item in (IEnumerable<IFACollectionViewGroup>)CollectionGroups)
			{
				(item as SpecializedCollectionViewGroup).HandleSortChanged();
			}
			OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
		finally
		{
			_ignoreGroupChanges = false;
		}
	}

	private void HandleFilterChanged()
	{
		try
		{
			_ignoreGroupChanges = true;
			if (_filter != null)
			{
				int num = 0;
				foreach (IFACollectionViewGroup item in (IEnumerable<IFACollectionViewGroup>)CollectionGroups)
				{
					num += (item as SpecializedCollectionViewGroup).HandleFilterChanged(_filter);
				}
				_count = num;
				OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
			else
			{
				Refresh();
			}
		}
		finally
		{
			_ignoreGroupChanges = false;
		}
	}

	private void OnVectorChanged(NotifyCollectionChangedEventArgs args)
	{
		CollectionChanged?.Invoke(this, args);
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
	}

	public void Add(object item)
	{
		ThrowICollectionViewNotMutableWhenGrouping();
	}

	public void Insert(int index, object item)
	{
		ThrowICollectionViewNotMutableWhenGrouping();
	}

	public bool Remove(object item)
	{
		ThrowICollectionViewNotMutableWhenGrouping();
		return false;
	}

	public void RemoveAt(int index)
	{
		ThrowICollectionViewNotMutableWhenGrouping();
	}

	public void Clear()
	{
		ThrowICollectionViewNotMutableWhenGrouping();
	}

	Task<FALoadMoreItemsResult> IFACollectionView.LoadMoreItemsAsync(uint count)
	{
		throw new NotImplementedException();
	}

	void IList.Insert(int index, object item)
	{
		ThrowICollectionViewNotMutableWhenGrouping();
	}

	void IList.RemoveAt(int index)
	{
		ThrowICollectionViewNotMutableWhenGrouping();
	}

	int IList.Add(object item)
	{
		ThrowICollectionViewNotMutableWhenGrouping();
		return -1;
	}

	void IList.Clear()
	{
		ThrowICollectionViewNotMutableWhenGrouping();
	}

	bool IList.Contains(object value)
	{
		return Contains(value);
	}

	void IList.Remove(object value)
	{
		Remove(value);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		CopyTo((object[])array, index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new GroupEnumerator(this);
	}

	private static void ThrowICollectionViewNotMutableWhenGrouping()
	{
		throw new InvalidOperationException("CollectionView is not mutable when grouping. Edit the source collection or group lists instead");
	}
}
