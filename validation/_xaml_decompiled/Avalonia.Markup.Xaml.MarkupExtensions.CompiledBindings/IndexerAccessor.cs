using System;
using System.Collections.Specialized;
using Avalonia.Data.Core;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;

internal class IndexerAccessor : InpcPropertyAccessor, IWeakEventSubscriber<NotifyCollectionChangedEventArgs>
{
	private readonly int _index;

	public IndexerAccessor(WeakReference<object?> target, IPropertyInfo basePropertyInfo, int argument)
		: base(target, basePropertyInfo)
	{
		_index = argument;
	}

	protected override void SubscribeCore()
	{
		base.SubscribeCore();
		if (_reference.TryGetTarget(out object target) && target is INotifyCollectionChanged target2)
		{
			WeakEvents.CollectionChanged.Subscribe(target2, this);
		}
	}

	protected override void UnsubscribeCore()
	{
		base.UnsubscribeCore();
		if (_reference.TryGetTarget(out object target) && target is INotifyCollectionChanged target2)
		{
			WeakEvents.CollectionChanged.Unsubscribe(target2, this);
		}
	}

	public void OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs args)
	{
		if (ShouldNotifyListeners(args))
		{
			SendCurrentValue();
		}
	}

	private bool ShouldNotifyListeners(NotifyCollectionChangedEventArgs e)
	{
		switch (e.Action)
		{
		case NotifyCollectionChangedAction.Add:
			return _index >= e.NewStartingIndex;
		case NotifyCollectionChangedAction.Remove:
			return _index >= e.OldStartingIndex;
		case NotifyCollectionChangedAction.Replace:
			if (_index >= e.NewStartingIndex)
			{
				return _index < e.NewStartingIndex + e.NewItems.Count;
			}
			return false;
		case NotifyCollectionChangedAction.Move:
			if (_index < e.NewStartingIndex || _index >= e.NewStartingIndex + e.NewItems.Count)
			{
				if (_index >= e.OldStartingIndex)
				{
					return _index < e.OldStartingIndex + e.OldItems.Count;
				}
				return false;
			}
			return true;
		case NotifyCollectionChangedAction.Reset:
			return true;
		default:
			return false;
		}
	}
}
