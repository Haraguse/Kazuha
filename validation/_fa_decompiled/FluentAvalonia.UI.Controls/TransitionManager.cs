using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace FluentAvalonia.UI.Controls;

internal sealed class TransitionManager
{
	private readonly FAItemsRepeater _owner;

	private FAItemCollectionTransitionProvider _transitionProvider;

	private bool _hasRecordedAdds;

	private bool _hasRecordedRemoves;

	private bool _hasRecordedResets;

	private bool _hasRecordedLayoutTransitions;

	public TransitionManager(FAItemsRepeater owner)
	{
		_owner = owner;
	}

	public void OnTransitionProviderChanged(FAItemCollectionTransitionProvider newProvider)
	{
		if (_transitionProvider != null)
		{
			_transitionProvider.TransitionCompleted -= OnTransitionProviderTransitionCompleted;
		}
		_transitionProvider = newProvider;
		if (newProvider != null)
		{
			newProvider.TransitionCompleted += OnTransitionProviderTransitionCompleted;
		}
	}

	public void OnLayoutChanging()
	{
		_hasRecordedLayoutTransitions = true;
	}

	public void OnItemsSourceChanged(object _, NotifyCollectionChangedEventArgs args)
	{
		switch (args.Action)
		{
		case NotifyCollectionChangedAction.Add:
			_hasRecordedAdds = true;
			break;
		case NotifyCollectionChangedAction.Remove:
			_hasRecordedRemoves = true;
			break;
		case NotifyCollectionChangedAction.Replace:
			_hasRecordedAdds = true;
			_hasRecordedRemoves = true;
			break;
		case NotifyCollectionChangedAction.Reset:
			_hasRecordedResets = true;
			break;
		case NotifyCollectionChangedAction.Move:
			break;
		}
	}

	public void OnElementPrepared(Control element)
	{
		if (_transitionProvider != null)
		{
			FAItemCollectionTransitionTriggers fAItemCollectionTransitionTriggers = (FAItemCollectionTransitionTriggers)0;
			if (_hasRecordedAdds)
			{
				fAItemCollectionTransitionTriggers |= FAItemCollectionTransitionTriggers.CollectionChangeAdd;
			}
			if (_hasRecordedResets)
			{
				fAItemCollectionTransitionTriggers |= FAItemCollectionTransitionTriggers.CollectionChangeReset;
			}
			if (_hasRecordedLayoutTransitions)
			{
				fAItemCollectionTransitionTriggers |= FAItemCollectionTransitionTriggers.LayoutTransition;
			}
			if (fAItemCollectionTransitionTriggers != 0)
			{
				_transitionProvider.QueueTransition(new FAItemCollectionTransition(_transitionProvider, element, FAItemCollectionTransitionOperation.Add, fAItemCollectionTransitionTriggers));
			}
		}
	}

	public bool ClearElement(Control element)
	{
		bool flag = false;
		if (_transitionProvider != null)
		{
			FAItemCollectionTransitionTriggers fAItemCollectionTransitionTriggers = (FAItemCollectionTransitionTriggers)0;
			if (_hasRecordedRemoves)
			{
				fAItemCollectionTransitionTriggers |= FAItemCollectionTransitionTriggers.CollectionChangeRemove;
			}
			if (_hasRecordedResets)
			{
				fAItemCollectionTransitionTriggers |= FAItemCollectionTransitionTriggers.CollectionChangeReset;
			}
			FAItemCollectionTransition transition = new FAItemCollectionTransition(_transitionProvider, element, FAItemCollectionTransitionOperation.Remove, fAItemCollectionTransitionTriggers);
			flag = fAItemCollectionTransitionTriggers != 0 && _transitionProvider.ShouldAnimate(transition);
			if (flag)
			{
				_transitionProvider.QueueTransition(transition);
			}
		}
		return flag;
	}

	public void OnElementBoundsChanged(Control element, Rect oldBounds, Rect newBounds)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (_transitionProvider != null)
		{
			FAItemCollectionTransitionTriggers fAItemCollectionTransitionTriggers = (FAItemCollectionTransitionTriggers)0;
			if (_hasRecordedAdds)
			{
				fAItemCollectionTransitionTriggers |= FAItemCollectionTransitionTriggers.CollectionChangeAdd;
			}
			if (_hasRecordedRemoves)
			{
				fAItemCollectionTransitionTriggers |= FAItemCollectionTransitionTriggers.CollectionChangeRemove;
			}
			if (_hasRecordedRemoves)
			{
				fAItemCollectionTransitionTriggers |= FAItemCollectionTransitionTriggers.CollectionChangeReset;
			}
			if (_hasRecordedLayoutTransitions)
			{
				fAItemCollectionTransitionTriggers |= FAItemCollectionTransitionTriggers.LayoutTransition;
			}
			if (fAItemCollectionTransitionTriggers == (FAItemCollectionTransitionTriggers)0)
			{
				fAItemCollectionTransitionTriggers = FAItemCollectionTransitionTriggers.LayoutTransition;
			}
			_transitionProvider.QueueTransition(new FAItemCollectionTransition(_transitionProvider, element, fAItemCollectionTransitionTriggers, oldBounds, newBounds));
		}
	}

	public void OnOwnerArranged()
	{
		_hasRecordedAdds = (_hasRecordedRemoves = (_hasRecordedLayoutTransitions = (_hasRecordedResets = false)));
	}

	private void OnTransitionProviderTransitionCompleted(FAItemCollectionTransitionProvider sender, FAItemCollectionTransitionCompletedEventArgs args)
	{
		if (args.Transition.Operation == FAItemCollectionTransitionOperation.Remove)
		{
			Control element = args.Element;
			if ((object)VisualExtensions.GetVisualParent((Visual)(object)element) == _owner)
			{
				_owner.ViewManager.ClearElementToElementFactory(element);
				((Layoutable)_owner).InvalidateArrange();
			}
		}
	}
}
