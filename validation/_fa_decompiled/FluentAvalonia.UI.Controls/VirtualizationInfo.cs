using System;
using System.Runtime.CompilerServices;
using Avalonia;

namespace FluentAvalonia.UI.Controls;

internal class VirtualizationInfo
{
	public enum ElementOwner
	{
		ElementFactory,
		Layout,
		PinnedPool,
		UniqueIdResetPool,
		Animator
	}

	[CompilerGenerated]
	private Rect _003CArrangeBounds_003Ek__BackingField;

	private uint _pinCounter;

	private int _index;

	private string _uniqueId;

	private ElementOwner _owner;

	private WeakReference<object> _data;

	internal const int PhaseReachedEnd = -1;

	public int Index => _index;

	public bool IsPinned => _pinCounter != 0;

	public bool IsHeldByLayout => _owner == ElementOwner.Layout;

	public bool IsRealized
	{
		get
		{
			if (!IsHeldByLayout)
			{
				return _owner == ElementOwner.PinnedPool;
			}
			return true;
		}
	}

	public bool IsInUniqueIdResetPool => _owner == ElementOwner.UniqueIdResetPool;

	public bool AutoRecycleCandidate { get; set; }

	public bool MustClearDataContext { get; set; }

	public bool CanBeScrollAnchor { get; set; }

	public ElementOwner Owner => _owner;

	public bool KeepAlive { get; set; }

	public Rect ArrangeBounds
	{
		[CompilerGenerated]
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _003CArrangeBounds_003Ek__BackingField;
		}
		[CompilerGenerated]
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			_003CArrangeBounds_003Ek__BackingField = value;
		}
	}

	public string UniqueId => _uniqueId;

	public object Data
	{
		get
		{
			if (_data != null)
			{
				if (!_data.TryGetTarget(out var target))
				{
					return null;
				}
				return target;
			}
			return null;
		}
	}

	public VirtualizationInfo()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		_index = -1;
		base._002Ector();
		ArrangeBounds = FAItemsRepeater.InvalidRect;
	}

	internal void UpdatePhasingInfo(object data)
	{
		_data = new WeakReference<object>(data);
	}

	internal void MoveOwnershipToLayoutFromElementFactory(int index, string uniqueId)
	{
		_owner = ElementOwner.Layout;
		_index = index;
		_uniqueId = uniqueId;
	}

	internal void MoveOwnershipToLayoutFromUniqueIdResetPool()
	{
		_owner = ElementOwner.Layout;
	}

	internal void MoveOwnershipToLayoutFromPinnedPool()
	{
		_owner = ElementOwner.Layout;
	}

	internal void MoveOwnershipToElementFactory()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		_owner = ElementOwner.ElementFactory;
		_pinCounter = 0u;
		_index = -1;
		_uniqueId = null;
		ArrangeBounds = FAItemsRepeater.InvalidRect;
	}

	internal void MoveOwnershipToUniqueIdResetPoolFromLayout()
	{
		_owner = ElementOwner.UniqueIdResetPool;
	}

	internal void MoveOwnershipToAnimator()
	{
		_owner = ElementOwner.Animator;
		_index = -1;
		_pinCounter = 0u;
	}

	internal void MoveOwnershipToPinnedPool()
	{
		_owner = ElementOwner.PinnedPool;
	}

	internal uint AddPin()
	{
		if (!IsRealized)
		{
			throw new InvalidOperationException("You can't pin an unrealized element");
		}
		return ++_pinCounter;
	}

	internal uint RemovePin()
	{
		if (!IsRealized)
		{
			throw new InvalidOperationException("You can't unpin an unrealized element");
		}
		if (!IsPinned)
		{
			throw new InvalidOperationException("UnpinElement was called more often than PinElement");
		}
		return --_pinCounter;
	}

	internal void UpdateIndex(int newIndex)
	{
		_index = newIndex;
	}
}
