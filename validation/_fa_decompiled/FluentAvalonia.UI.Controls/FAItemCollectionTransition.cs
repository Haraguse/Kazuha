using System;
using Avalonia;
using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

public class FAItemCollectionTransition
{
	private WeakReference<FAItemCollectionTransitionProvider> _owningProvider;

	private Control _element;

	private FAItemCollectionTransitionOperation _operation;

	private FAItemCollectionTransitionTriggers _triggers;

	private Rect _oldBounds;

	private Rect _newBounds;

	private FAItemCollectionTransitionProgress _progress;

	public FAItemCollectionTransitionProvider OwningProvider
	{
		get
		{
			if (!_owningProvider.TryGetTarget(out var target))
			{
				return null;
			}
			return target;
		}
	}

	public Control Element => _element;

	public bool HasStarted => _progress != null;

	public FAItemCollectionTransitionOperation Operation => _operation;

	public FAItemCollectionTransitionTriggers Triggers => _triggers;

	public FAItemCollectionTransition(FAItemCollectionTransitionProvider provider, Control element, FAItemCollectionTransitionOperation operation, FAItemCollectionTransitionTriggers triggers)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		this._002Ector(provider, element, operation, triggers, default(Rect), default(Rect));
	}

	public FAItemCollectionTransition(FAItemCollectionTransitionProvider provider, Control element, FAItemCollectionTransitionTriggers triggers, Rect oldBounds, Rect newBounds)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		this._002Ector(provider, element, FAItemCollectionTransitionOperation.Move, triggers, oldBounds, newBounds);
	}

	public FAItemCollectionTransition(FAItemCollectionTransitionProvider provider, Control element, FAItemCollectionTransitionOperation operation, FAItemCollectionTransitionTriggers triggers, Rect oldBounds, Rect newBounds)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		base._002Ector();
		_owningProvider = new WeakReference<FAItemCollectionTransitionProvider>(provider);
		_element = element;
		_operation = operation;
		_triggers = triggers;
		_oldBounds = oldBounds;
		_newBounds = newBounds;
	}

	public FAItemCollectionTransitionProgress Start()
	{
		if (_progress == null)
		{
			_progress = new FAItemCollectionTransitionProgress(this);
		}
		return _progress;
	}
}
