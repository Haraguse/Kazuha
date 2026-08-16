using System;
using System.Collections.Generic;
using Avalonia.Threading;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

public abstract class FAItemCollectionTransitionProvider
{
	private List<FAItemCollectionTransition> _transitions;

	private List<FAItemCollectionTransition> _transitionsWithAnimations;

	private bool _rendering;

	public event TypedEventHandler<FAItemCollectionTransitionProvider, FAItemCollectionTransitionCompletedEventArgs> TransitionCompleted;

	public void QueueTransition(FAItemCollectionTransition transition)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		if (_transitions == null)
		{
			_transitions = new List<FAItemCollectionTransition>();
		}
		if (_transitionsWithAnimations == null)
		{
			_transitionsWithAnimations = new List<FAItemCollectionTransition>();
		}
		if (FAUISettings.AreAnimationsEnabled() && ShouldAnimate(transition))
		{
			_transitionsWithAnimations.Add(transition);
		}
		_transitions.Add(transition);
		if (!_rendering)
		{
			Dispatcher.UIThread.Post((Action)OnRendering, DispatcherPriority.Render);
		}
	}

	public bool ShouldAnimate(FAItemCollectionTransition transition)
	{
		return ShouldAnimateCore(transition);
	}

	public abstract void StartTransitions(IList<FAItemCollectionTransition> transitions);

	public abstract bool ShouldAnimateCore(FAItemCollectionTransition transition);

	public void NotifyTransitionComplete(FAItemCollectionTransition transition)
	{
		TransitionCompleted?.Invoke(this, new FAItemCollectionTransitionCompletedEventArgs(transition));
	}

	private void OnRendering()
	{
		_rendering = false;
		try
		{
			StartTransitions(_transitionsWithAnimations);
			foreach (FAItemCollectionTransition transition in _transitions)
			{
				if (!transition.HasStarted)
				{
					NotifyTransitionComplete(transition);
				}
			}
		}
		finally
		{
			ResetState();
		}
	}

	private void ResetState()
	{
		_transitions.Clear();
		_transitionsWithAnimations.Clear();
	}
}
