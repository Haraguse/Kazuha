using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Data;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Data;

internal class SpecializedCollectionViewGroup : CollectionViewGroup, IComparer<object>
{
	private List<object> _view;

	private IEnumerable _actualItems;

	private static BindingEvaluator<object> _bindingHelper;

	private bool _ignoreNotify;

	public SpecializedCollectionViewGroup(FAGroupedDataCollectionView owner, object item, bool hasItemsBinding)
		: base(owner, item, hasItemsBinding)
	{
	}

	internal int HandleFilterChanged(Predicate<object> filter)
	{
		if (filter != null)
		{
			for (int i = 0; i < _view.Count; i++)
			{
				object obj = _view.ElementAt(i);
				if (!filter(obj))
				{
					RemoveFromView(i, obj);
					i--;
				}
			}
			HashSet<object> hashSet = new HashSet<object>(_view);
			int num = 0;
			for (int j = 0; j < _actualItems.Count(); j++)
			{
				object obj2 = _actualItems.ElementAt(j);
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
		return _view.Count;
	}

	internal void HandleSortChanged()
	{
		_view.Sort(this);
	}

	internal int Refresh()
	{
		_view.Clear();
		Predicate<object> filter = _owner.Filter;
		IList<FASortDescription> sortDescriptions = _owner.GetSortDescriptions();
		foreach (object actualItem in _actualItems)
		{
			if (filter != null && !filter(actualItem))
			{
				continue;
			}
			if (sortDescriptions != null && sortDescriptions.Count > 0)
			{
				int num = _view.BinarySearch(actualItem, this);
				if (num < 0)
				{
					num = ~num;
				}
				_view.Insert(num, actualItem);
			}
			else
			{
				_view.Add(actualItem);
			}
		}
		return _view.Count;
	}

	protected override void Init()
	{
		if (!_hasItemsBinding)
		{
			_actualItems = new CollectionWrapper(base.Group as IEnumerable);
		}
		else
		{
			_actualItems = new CollectionWrapper(_owner.GetItemsFromGroup(base.Group));
		}
		(_actualItems as INotifyCollectionChanged).CollectionChanged += OnGroupItemsCollectionChanged;
		AttachPropertyChangedHandler(_actualItems);
		_view = new List<object>(_actualItems.Count());
		base.GroupItems = _view;
		SourceChanged();
	}

	internal override void UpdateGroup(object group)
	{
		DetachPropertyChangedHandler(_actualItems);
		_ignoreNotify = true;
		Init();
		_ignoreNotify = false;
	}

	protected override void OnGroupItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		switch (args.Action)
		{
		case NotifyCollectionChangedAction.Add:
			AttachPropertyChangedHandler(args.NewItems);
			if (_owner.DeferCounter <= 0)
			{
				if (args.NewItems.Count == 1)
				{
					HandleItemAdded(args.NewStartingIndex, args.NewItems[0]);
				}
				else
				{
					SourceChanged();
				}
			}
			break;
		case NotifyCollectionChangedAction.Remove:
			DetachPropertyChangedHandler(args.OldItems);
			if (_owner.DeferCounter <= 0)
			{
				if (args.OldItems.Count == 1)
				{
					HandleItemRemoved(args.OldStartingIndex, args.OldItems[0]);
				}
				else
				{
					SourceChanged();
				}
			}
			break;
		default:
			SourceChanged();
			break;
		}
	}

	private void AttachPropertyChangedHandler(IEnumerable items)
	{
		if (!_owner.IsLiveShapingEnabled || items == null)
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
		if (!_owner.IsLiveShapingEnabled || items == null)
		{
			return;
		}
		foreach (INotifyPropertyChanged item in items.OfType<INotifyPropertyChanged>())
		{
			item.PropertyChanged -= ItemOnPropertyChanged;
		}
	}

	private void ItemOnPropertyChanged(object item, PropertyChangedEventArgs args)
	{
		if (!_owner.IsLiveShapingEnabled)
		{
			return;
		}
		Predicate<object> filter = _owner.Filter;
		HashSet<string> filterProperties = _owner.GetFilterProperties();
		IList<FASortDescription> sortDescriptions = _owner.GetSortDescriptions();
		bool? flag = filter?.Invoke(item);
		if (flag.HasValue && filterProperties.Contains(args.PropertyName))
		{
			int num = _view.IndexOf(item);
			if (num != -1 && !flag.Value)
			{
				RemoveFromView(num, item);
			}
			else if (num == -1 && flag.Value)
			{
				int newStartingIndex = _actualItems.IndexOf(item);
				HandleItemAdded(newStartingIndex, item);
			}
		}
		if ((flag ?? true) && sortDescriptions != null && sortDescriptions.Any((FASortDescription sd) => sd.PropertyName == args.PropertyName))
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
		}
		else if (string.IsNullOrEmpty(args.PropertyName))
		{
			SourceChanged();
		}
	}

	private void SourceChanged()
	{
		_view.Clear();
		Predicate<object> filter = _owner.Filter;
		IList<FASortDescription> sortDescriptions = _owner.GetSortDescriptions();
		foreach (object actualItem in _actualItems)
		{
			if (filter != null && !filter(actualItem))
			{
				continue;
			}
			if (sortDescriptions != null && sortDescriptions.Count > 0)
			{
				int num = _view.BinarySearch(actualItem, this);
				if (num < 0)
				{
					num = ~num;
				}
				_view.Insert(num, actualItem);
			}
			else
			{
				_view.Add(actualItem);
			}
		}
		OnVectorChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	private void OnVectorChanged(NotifyCollectionChangedEventArgs args)
	{
		if (!_ignoreNotify)
		{
			_owner.GroupItemsChanged(this, args);
		}
	}

	private bool HandleItemAdded(int newStartingIndex, object newItem, int? viewIndex = null)
	{
		Predicate<object> filter = _owner.Filter;
		IList<FASortDescription> sortDescriptions = _owner.GetSortDescriptions();
		if (filter != null && !filter(newItem))
		{
			return false;
		}
		int num = _view.Count;
		if (sortDescriptions != null && sortDescriptions.Count > 0)
		{
			num = _view.BinarySearch(newItem, this);
			if (num < 0)
			{
				num = ~num;
			}
		}
		else if (filter != null)
		{
			if (_actualItems == null)
			{
				SourceChanged();
				return false;
			}
			if (newStartingIndex == 0 || _view.Count == 0)
			{
				num = 0;
			}
			else if (newStartingIndex == _actualItems.Count() - 1)
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
				for (; i < _actualItems.Count(); i++)
				{
					if (i == newStartingIndex)
					{
						num = num2;
						break;
					}
					if (_view[num2] == _actualItems.ElementAt(i))
					{
						num2++;
					}
				}
			}
		}
		_view.Insert(num, newItem);
		NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, num);
		OnVectorChanged(args);
		return true;
	}

	private void HandleItemRemoved(int index, object item)
	{
		Predicate<object> filter = _owner.Filter;
		if (filter == null || filter(item))
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
		NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
		OnVectorChanged(args);
	}

	int IComparer<object>.Compare(object x, object y)
	{
		IList<FASortDescription> sortDescriptions = _owner.GetSortDescriptions();
		if (sortDescriptions != null)
		{
			for (int i = 0; i < sortDescriptions.Count; i++)
			{
				FASortDescription fASortDescription = sortDescriptions[i];
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

	private static object EvaluateBinding(BindingBase binding, object item)
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
}
