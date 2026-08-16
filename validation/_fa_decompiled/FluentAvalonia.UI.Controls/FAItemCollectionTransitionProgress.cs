using System;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

public class FAItemCollectionTransitionProgress
{
	private WeakReference<FAItemCollectionTransition> _transition;

	public FAItemCollectionTransitionProgress(FAItemCollectionTransition transition)
	{
		_transition = new WeakReference<FAItemCollectionTransition>(transition);
	}

	public Control Element()
	{
		if (!_transition.TryGetTarget(out var target))
		{
			return null;
		}
		return target.Element;
	}

	public void Complete()
	{
		if (_transition.TryGetTarget(out var target))
		{
			target.OwningProvider.NotifyTransitionComplete(target);
		}
	}
}
