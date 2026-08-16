using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace FluentAvalonia.UI.Data;

internal class CollectionViewGroup : IFACollectionViewGroup
{
	protected FAGroupedDataCollectionView _owner;

	protected bool _hasItemsBinding;

	public object Group { get; private set; }

	public IList<object> GroupItems { get; protected set; }

	public CollectionViewGroup(FAGroupedDataCollectionView owner, object item, bool hasItemsBinding)
	{
		_owner = owner;
		Group = item;
		_hasItemsBinding = hasItemsBinding;
		Init();
	}

	internal virtual void UpdateGroup(object group)
	{
		Group = group;
		if (!_hasItemsBinding)
		{
			GroupItems = new CollectionWrapper(group as IEnumerable);
		}
		else
		{
			GroupItems = new CollectionWrapper(_owner.GetItemsFromGroup(group));
		}
	}

	protected virtual void Init()
	{
		if (!_hasItemsBinding)
		{
			GroupItems = new CollectionWrapper(Group as IEnumerable);
		}
		else
		{
			GroupItems = new CollectionWrapper(_owner.GetItemsFromGroup(Group));
		}
		(GroupItems as INotifyCollectionChanged).CollectionChanged += OnGroupItemsCollectionChanged;
	}

	protected virtual void OnGroupItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		_owner.GroupItemsChanged(this, args);
	}
}
