using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Data;

public sealed class FAIterableCollectionView : IFACollectionView, IEnumerable<object>, IEnumerable, IList<object>, ICollection<object>, INotifyCollectionChanged, IFAAdvancedCollectionView, INotifyPropertyChanged, IList, ICollection, IComparer<object>
{
	private IList<FASortDescription> _sortDescriptions;

	private Predicate<object> _filter;

	private IEnumerable _source;

	private ItemsSourceView _sourceView;

	private List<object> _view;

	private HashSet<string> _filterProperties;

	private int _deferCounter;

	private static BindingEvaluator<object> _bindingHelper;

	private bool _hasFilterOrSort;

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

	public object this[int index]
	{
		get
		{
			if (_view == null)
			{
				return _sourceView[index];
			}
			return _view[index];
		}
		set
		{
			if (!(_source is IList))
			{
				ThrowForNonMutableSource();
			}
			((IList)_source)[index] = value;
		}
	}

	public int Count
	{
		get
		{
			if (_view == null)
			{
				return _sourceView.Count;
			}
			return _view.Count;
		}
	}

	public int CurrentPosition { get; private set; }

	public bool HasMoreItems { get; private set; }

	public bool IsCurrentAfterLast => CurrentPosition >= Count;

	public bool IsCurrentBeforeFirst => CurrentPosition < 0;

	public object CurrentItem
	{
		get
		{
			int num = _view?.Count ?? _sourceView.Count;
			int currentPosition = CurrentPosition;
			if (currentPosition < 0 || currentPosition >= num)
			{
				return null;
			}
			if (_view != null)
			{
				return _view[currentPosition];
			}
			return _sourceView[currentPosition];
		}
	}

	public bool IsReadOnly
	{
		get
		{
			if (!(_source is IList list))
			{
				return false;
			}
			return list.IsReadOnly;
		}
	}

	internal IEnumerable Source => _source;

	IAvaloniaList<IFACollectionViewGroup> IFACollectionView.CollectionGroups => null;

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

	int ICollection.Count => Count;

	public event EventHandler<object> CurrentChanged;

	public event FACurrentChangingEventHandler CurrentChanging;

	public event NotifyCollectionChangedEventHandler CollectionChanged;

	public event PropertyChangedEventHandler PropertyChanged;

	public FAIterableCollectionView(IEnumerable collection)
		: this(collection, isLiveShaping: false, null, null, null)
	{
	}

	public FAIterableCollectionView(IEnumerable collection, bool isLiveShaping)
		: this(collection, isLiveShaping, null, null, null)
	{
	}

	public FAIterableCollectionView(IEnumerable collection, Predicate<object> filter)
		: this(collection, isLiveShaping: false, filter, null, null)
	{
	}

	public FAIterableCollectionView(IEnumerable collection, Predicate<object> filter, IList<string> filterProperties)
		: this(collection, isLiveShaping: true, filter, filterProperties, null)
	{
	}

	public FAIterableCollectionView(IEnumerable collection, IList<FASortDescription> sortDescriptions)
		: this(collection, isLiveShaping: false, null, null, sortDescriptions)
	{
	}

	public FAIterableCollectionView(IEnumerable collection, bool isLiveShaping, Predicate<object> filter, IList<string> filterProperties, IList<FASortDescription> sortDescriptions)
	{
		collection = collection ?? throw new ArgumentNullException("collection");
		_source = collection;
		_sourceView = ItemsSourceView.GetOrCreate(collection);
		_sourceView.CollectionChanged += SourceCollectionChanged;
		IsLiveShapingEnabled = isLiveShaping;
		if ((filter != null || sortDescriptions != null) | isLiveShaping)
		{
			_hasFilterOrSort = true;
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
			_view = new List<object>(_sourceView.Count);
			AttachPropertyChangedHandler(_source);
			HandleSourceChanged();
			OnPropertyChanged(".ctor");
		}
	}

	public void Add(object item)
	{
		Insert(Count, item);
	}

	public void Clear()
	{
		if (IsReadOnly || !(_source is IList))
		{
			ThrowForNonMutableSource();
		}
		(_source as IList).Clear();
	}

	public bool Contains(object item)
	{
		return _view?.Contains(item) ?? _source.Contains(item);
	}

	public void CopyTo(object[] array, int arrayIndex)
	{
		if (_view != null)
		{
			_view.CopyTo(array, arrayIndex);
			return;
		}
		IEnumerator enumerator = _source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			array[arrayIndex++] = enumerator.Current;
		}
	}

	public IEnumerator<object> GetEnumerator()
	{
		if (_view != null)
		{
			return _view.GetEnumerator();
		}
		return Enumerate(_source);
		static IEnumerator<object> Enumerate(IEnumerable items)
		{
			if (items != null)
			{
				foreach (object item in items)
				{
					yield return item;
				}
			}
		}
	}

	public int IndexOf(object item)
	{
		if (_view != null)
		{
			return _view.IndexOf(item);
		}
		return _source.IndexOf(item);
	}

	public void Insert(int index, object item)
	{
		if (!(_source is IList) || IsReadOnly)
		{
			ThrowForNonMutableSource();
		}
		((IList)_source).Insert(index, item);
	}

	public bool MoveCurrentTo(object item)
	{
		if (item != CurrentItem)
		{
			return MoveCurrentToPosition(IndexOf(item));
		}
		return true;
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

	public bool Remove(object item)
	{
		if (IsReadOnly || !(_source is IList))
		{
			ThrowForNonMutableSource();
		}
		((IList)_source).Remove(item);
		return true;
	}

	public void RemoveAt(int index)
	{
		if (IsReadOnly || !(_source is IList))
		{
			ThrowForNonMutableSource();
		}
		((IList)_source).RemoveAt(index);
	}

	/// <inheritdoc />
	public void Refresh()
	{
		HandleSourceChanged();
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

	public IDisposable DeferRefresh()
	{
		_deferCounter++;
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
		_deferCounter--;
		if (_deferCounter == 0)
		{
			MoveCurrentTo(lastCurrentItem);
			Refresh();
		}
	}

	private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		if (_hasFilterOrSort)
		{
			switch (args.Action)
			{
			case NotifyCollectionChangedAction.Add:
				AttachPropertyChangedHandler(args.NewItems);
				if (_deferCounter <= 0)
				{
					if (args.NewItems.Count == 1)
					{
						HandleItemAdded(args.NewStartingIndex, args.NewItems[0]);
					}
					else
					{
						HandleSourceChanged();
					}
				}
				break;
			case NotifyCollectionChangedAction.Remove:
				DetachPropertyChangedHandler(args.OldItems);
				if (_deferCounter <= 0)
				{
					if (args.OldItems.Count == 1)
					{
						HandleItemRemoved(args.OldStartingIndex, args.OldItems[0]);
					}
					else
					{
						HandleSourceChanged();
					}
				}
				break;
			default:
				HandleSourceChanged();
				break;
			}
		}
		else
		{
			OnVectorChanged(args);
		}
	}

	private void OnSortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		if (_deferCounter <= 0)
		{
			HandleSortChanged();
		}
	}

	private void ItemOnPropertyChanged(object item, PropertyChangedEventArgs args)
	{
		if (!IsLiveShapingEnabled)
		{
			return;
		}
		bool? flag = _filter?.Invoke(item);
		if (flag.HasValue && _filterProperties.Contains(args.PropertyName))
		{
			int num = _view.IndexOf(item);
			if (num != -1 && !flag.Value)
			{
				RemoveFromView(num, item);
			}
			else if (num == -1 && flag.Value)
			{
				int newStartingIndex = _source.IndexOf(item);
				HandleItemAdded(newStartingIndex, item);
			}
		}
		if (flag ?? true)
		{
			IList<FASortDescription> sortDescriptions = _sortDescriptions;
			if (sortDescriptions != null && sortDescriptions.Any((FASortDescription sd) => sd.PropertyName == args.PropertyName))
			{
				int num2 = _view.IndexOf(item);
				if (num2 >= 0)
				{
					_view.RemoveAt(num2);
					int num3 = _view.BinarySearch(item, this);
					if (num3 < 0)
					{
						num3 = ~num3;
					}
					if (num3 != num2)
					{
						OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, num2));
						_view.Insert(num3, item);
						OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, num3));
					}
					else
					{
						_view.Insert(num3, item);
					}
				}
				return;
			}
		}
		if (string.IsNullOrEmpty(args.PropertyName))
		{
			HandleSourceChanged();
		}
	}

	private void AttachPropertyChangedHandler(IEnumerable items)
	{
		if (!IsLiveShapingEnabled || items == null)
		{
			return;
		}
		foreach (INotifyPropertyChanged item in items.OfType<INotifyPropertyChanged>())
		{
			item.PropertyChanged += ItemOnPropertyChanged;
		}
	}

	private void DetachPropertyChangedHandler(IEnumerable items)
	{
		if (!IsLiveShapingEnabled || items == null)
		{
			return;
		}
		foreach (INotifyPropertyChanged item in items.OfType<INotifyPropertyChanged>())
		{
			item.PropertyChanged -= ItemOnPropertyChanged;
		}
	}

	private void HandleSourceChanged()
	{
		object currentItem = CurrentItem;
		_view.Clear();
		foreach (object item in _source)
		{
			if (_filter != null && !_filter(item))
			{
				continue;
			}
			if (_sortDescriptions != null && _sortDescriptions.Count > 0)
			{
				int num = _view.BinarySearch(item, this);
				if (num < 0)
				{
					num = ~num;
				}
				_view.Insert(num, item);
			}
			else
			{
				_view.Add(item);
			}
		}
		OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		MoveCurrentTo(currentItem);
	}

	private void HandleSortChanged()
	{
		_view.Sort(this);
		OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	private void HandleFilterChanged()
	{
		if (_view == null)
		{
			_view = new List<object>();
		}
		if (_filter != null)
		{
			for (int i = 0; i < _view.Count; i++)
			{
				object obj = _view.ElementAt(i);
				if (!_filter(obj))
				{
					RemoveFromView(i, obj);
					i--;
				}
			}
			HashSet<object> hashSet = new HashSet<object>(_view);
			int num = 0;
			for (int j = 0; j < _sourceView.Count; j++)
			{
				object obj2 = _sourceView[j];
				if (hashSet.Contains(obj2))
				{
					num++;
				}
				else if (HandleItemAdded(j, obj2, num))
				{
					num++;
				}
			}
		}
		else
		{
			Refresh();
		}
	}

	private bool HandleItemAdded(int newStartingIndex, object newItem, int? viewIndex = null)
	{
		if (_filter != null && !_filter(newItem))
		{
			return false;
		}
		int num = _view.Count;
		if (_sortDescriptions != null && _sortDescriptions.Count > 0)
		{
			num = _view.BinarySearch(newItem, this);
			if (num < 0)
			{
				num = ~num;
			}
		}
		else if (_filter != null)
		{
			if (_source == null)
			{
				HandleSourceChanged();
				return false;
			}
			if (newStartingIndex == 0 || _view.Count == 0)
			{
				num = 0;
			}
			else if (newStartingIndex == _sourceView.Count - 1)
			{
				num = _view.Count - 1;
			}
			else if (viewIndex.HasValue)
			{
				num = viewIndex.Value;
			}
			else
			{
				int i = 0;
				int num2 = 0;
				for (; i < _sourceView.Count; i++)
				{
					if (i == newStartingIndex)
					{
						num = num2;
						break;
					}
					if (_view[num2] == _sourceView[i])
					{
						num2++;
					}
				}
			}
		}
		_view.Insert(num, newItem);
		if (num <= CurrentPosition)
		{
			CurrentPosition++;
		}
		NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, num);
		OnVectorChanged(args);
		return true;
	}

	private void HandleItemRemoved(int index, object item)
	{
		if (_filter == null || _filter(item))
		{
			if (index < 0 || index >= _view.Count || !object.Equals(_view[index], item))
			{
				index = _view.IndexOf(item);
			}
			if (index >= 0)
			{
				RemoveFromView(index, item);
			}
		}
	}

	private void RemoveFromView(int index, object item)
	{
		_view.RemoveAt(index);
		if (index <= CurrentPosition)
		{
			CurrentPosition--;
		}
		NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
		OnVectorChanged(args);
	}

	private void OnVectorChanged(NotifyCollectionChangedEventArgs args)
	{
		if (_deferCounter <= 0)
		{
			CollectionChanged?.Invoke(this, args);
			OnPropertyChanged("Count");
		}
	}

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private object EvaluateBinding(BindingBase binding, object item)
	{
		if (_bindingHelper == null)
		{
			_bindingHelper = new BindingEvaluator<object>();
		}
		_bindingHelper.UpdateBinding(binding);
		object result = _bindingHelper.Evaluate(item);
		_bindingHelper.ClearDataContext();
		return result;
	}

	int IComparer<object>.Compare(object x, object y)
	{
		if (_sortDescriptions != null)
		{
			for (int i = 0; i < _sortDescriptions.Count; i++)
			{
				FASortDescription fASortDescription = _sortDescriptions[i];
				object x2;
				object y2;
				if (fASortDescription.Property == null)
				{
					x2 = x;
					y2 = y;
				}
				else
				{
					x2 = EvaluateBinding(fASortDescription.Property, x);
					y2 = EvaluateBinding(fASortDescription.Property, y);
				}
				int num = fASortDescription.Comparer.Compare(x2, y2);
				if (num != 0)
				{
					if (fASortDescription.Direction != FASortDirection.Ascending)
					{
						return -num;
					}
					return num;
				}
			}
		}
		return 0;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	Task<FALoadMoreItemsResult> IFACollectionView.LoadMoreItemsAsync(uint count)
	{
		throw new NotImplementedException();
	}

	void IList.Insert(int index, object item)
	{
		Insert(index, item);
	}

	int IList.Add(object item)
	{
		Add(item);
		return Count - 1;
	}

	void IList.Clear()
	{
		Clear();
	}

	bool IList.Contains(object value)
	{
		return Contains(value);
	}

	void IList.Remove(object item)
	{
		Remove(item);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		CopyTo((object[])array, index);
	}

	private static void ThrowForNonMutableSource()
	{
		throw new NotSupportedException("Underlying source of type {_collection.GetType()} is not mutable. Source collectionmust implement non-generic IList for CollectionView mutation");
	}
}
