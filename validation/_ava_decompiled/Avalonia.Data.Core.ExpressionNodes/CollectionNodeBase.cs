using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.ExpressionNodes;

internal abstract class CollectionNodeBase : ExpressionNode, IWeakEventSubscriber<NotifyCollectionChangedEventArgs>, IWeakEventSubscriber<PropertyChangedEventArgs>
{
	void IWeakEventSubscriber<NotifyCollectionChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, NotifyCollectionChangedEventArgs e)
	{
		if (ShouldUpdate(sender, e))
		{
			UpdateValueOrSetError(sender);
		}
	}

	void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
	{
		if (ShouldUpdate(sender, e))
		{
			UpdateValueOrSetError(sender);
		}
	}

	protected override void OnSourceChanged(object? source, Exception? dataValidationError)
	{
		if (ValidateNonNullSource(source))
		{
			Subscribe(source);
			UpdateValue(source);
		}
	}

	protected override void Unsubscribe(object source)
	{
		if (source is INotifyCollectionChanged target)
		{
			WeakEvents.CollectionChanged.Unsubscribe(target, this);
		}
		if (source is INotifyPropertyChanged target2)
		{
			WeakEvents.ThreadSafePropertyChanged.Unsubscribe(target2, this);
		}
	}

	protected abstract bool ShouldUpdate(object? sender, PropertyChangedEventArgs e);

	protected abstract int? TryGetFirstArgumentAsInt();

	protected abstract void UpdateValue(object? source);

	private bool ShouldUpdate(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (sender != base.Source)
		{
			return false;
		}
		if (sender is IList)
		{
			int? num = TryGetFirstArgumentAsInt();
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				return e.Action switch
				{
					NotifyCollectionChangedAction.Add => valueOrDefault >= e.NewStartingIndex, 
					NotifyCollectionChangedAction.Remove => valueOrDefault >= e.OldStartingIndex, 
					NotifyCollectionChangedAction.Replace => valueOrDefault >= e.NewStartingIndex && valueOrDefault < e.NewStartingIndex + e.NewItems.Count, 
					NotifyCollectionChangedAction.Move => (valueOrDefault >= e.NewStartingIndex && valueOrDefault < e.NewStartingIndex + e.NewItems.Count) || (valueOrDefault >= e.OldStartingIndex && valueOrDefault < e.OldStartingIndex + e.OldItems.Count), 
					_ => true, 
				};
			}
		}
		return true;
	}

	private void Subscribe(object? source)
	{
		if (source is INotifyCollectionChanged target)
		{
			WeakEvents.CollectionChanged.Subscribe(target, this);
		}
		if (source is INotifyPropertyChanged target2)
		{
			WeakEvents.ThreadSafePropertyChanged.Subscribe(target2, this);
		}
	}

	private void UpdateValueOrSetError(object? source)
	{
		try
		{
			UpdateValue(source);
		}
		catch (Exception error)
		{
			SetError(error);
		}
	}
}
