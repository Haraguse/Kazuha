using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace FluentAvalonia.UI.Controls;

internal class ViewManager
{
	private struct PinnedElementInfo(Control element, VirtualizationInfo vi = null)
	{
		public Control PinnedElement { get; } = element;

		public VirtualizationInfo VirtualizationInfo { get; } = vi ?? FAItemsRepeater.GetVirtualizationInfo(element);
	}

	private readonly FAItemsRepeater _owner;

	private readonly List<PinnedElementInfo> _pinnedPool;

	private readonly UniqueIdElementPool _resetPool;

	private Control _lastFocusedElement;

	private bool _isDataSourceStableResetPending;

	private Phaser _phaser;

	private readonly FAElementFactoryGetArgs _elementFactoryGetArgs = new FAElementFactoryGetArgs();

	private readonly FAElementFactoryRecycleArgs _elementFactoryRecycleArgs = new FAElementFactoryRecycleArgs();

	private int _firstRealizedElementIndexHeldByLayout = int.MaxValue;

	private int _lastRealizedElementIndexHeldByLayout = int.MinValue;

	private const int FirstRealizedElementIndexDefault = int.MaxValue;

	private const int LastRealizedElementIndexDefault = int.MinValue;

	private bool _gotFocus;

	internal int FirstRealizedIndex => _firstRealizedElementIndexHeldByLayout;

	internal int LastRealizedIndex => _lastRealizedElementIndexHeldByLayout;

	public ViewManager(FAItemsRepeater ir)
	{
		_owner = ir;
		_resetPool = new UniqueIdElementPool(ir);
		_phaser = new Phaser(ir);
		_pinnedPool = new List<PinnedElementInfo>();
	}

	public Control GetElement(int index, bool forceCreate, bool suppressAutoRecycle)
	{
		bool flag = false;
		Control val = (forceCreate ? null : GetElementIfAlreadyHeldByLayout(index));
		if (val == null)
		{
			Control madeAnchor = _owner.MadeAnchor;
			if (madeAnchor != null && FAItemsRepeater.TryGetVirtualizationInfo(madeAnchor).Index == index)
			{
				val = madeAnchor;
				flag = true;
			}
		}
		if (val == null)
		{
			val = GetElementFromUniqueIdResetPool(index);
		}
		if ((val == null) | flag)
		{
			Control elementFromPinnedElements = GetElementFromPinnedElements(index);
			if (val == null && elementFromPinnedElements != null)
			{
				val = elementFromPinnedElements;
			}
		}
		if (val == null)
		{
			val = GetElementFromElementFactory(index);
		}
		VirtualizationInfo virtualizationInfo = FAItemsRepeater.TryGetVirtualizationInfo(val);
		if (suppressAutoRecycle)
		{
			virtualizationInfo.AutoRecycleCandidate = false;
		}
		else
		{
			virtualizationInfo.AutoRecycleCandidate = true;
			virtualizationInfo.KeepAlive = true;
		}
		return val;
	}

	public void ClearElement(Control element, bool isClearedDueToCollectionChange)
	{
		VirtualizationInfo virtualizationInfo = FAItemsRepeater.GetVirtualizationInfo(element);
		int index = virtualizationInfo.Index;
		if (!ClearElementToUniqueIdResetPool(element, virtualizationInfo) && !ClearElementToAnimator(element, virtualizationInfo) && !ClearElementToPinnedPool(element, virtualizationInfo, isClearedDueToCollectionChange))
		{
			ClearElementToElementFactory(element);
		}
		if (index == _firstRealizedElementIndexHeldByLayout && index == _lastRealizedElementIndexHeldByLayout)
		{
			InvalidateRealizedIndicesHeldByLayout();
		}
		else if (index == _firstRealizedElementIndexHeldByLayout)
		{
			_firstRealizedElementIndexHeldByLayout++;
		}
		else if (index == _lastRealizedElementIndexHeldByLayout)
		{
			_lastRealizedElementIndexHeldByLayout--;
		}
	}

	internal void ClearElementToElementFactory(Control element)
	{
		_owner.OnElementClearing(element);
		VirtualizationInfo virtualizationInfo = FAItemsRepeater.GetVirtualizationInfo(element);
		virtualizationInfo.MoveOwnershipToElementFactory();
		if (virtualizationInfo.MustClearDataContext)
		{
			((StyledElement)element).DataContext = null;
		}
		if (_owner.ItemTemplateShim != null)
		{
			_elementFactoryRecycleArgs.Element = element;
			_elementFactoryRecycleArgs.Parent = (Control)(object)_owner;
			_owner.ItemTemplateShim.RecycleElement(_elementFactoryRecycleArgs);
			_elementFactoryRecycleArgs.Element = null;
			_elementFactoryRecycleArgs.Parent = null;
		}
		else
		{
			Controls children = ((Panel)_owner).Children;
			int num = ((AvaloniaList<Control>)(object)children).IndexOf(element);
			((AvaloniaList<Control>)(object)children).RemoveAt(num);
		}
		_phaser.StopPhasing(element, virtualizationInfo);
		if (_lastFocusedElement == element)
		{
			int index = virtualizationInfo.Index;
			MoveFocusFromClearedIndex(index);
		}
	}

	private void MoveFocusFromClearedIndex(int clearedIndex)
	{
		Control val = FindFocusCandidate(clearedIndex, out var focusedChild);
		if (val != null)
		{
			_ = _lastFocusedElement;
			((InputElement)val).Focus((NavigationMethod)0, (KeyModifiers)0);
			_lastFocusedElement = val;
			UpdatePin(focusedChild, addPin: true);
		}
		else
		{
			_lastFocusedElement = null;
		}
	}

	private Control FindFocusCandidate(int clearedIndex, out Control focusedChild)
	{
		focusedChild = null;
		int num = int.MinValue;
		int num2 = int.MaxValue;
		Control val = null;
		Control val2 = null;
		Controls children = ((Panel)_owner).Children;
		for (int i = 0; i < ((AvaloniaList<Control>)(object)children).Count; i++)
		{
			Control val3 = ((AvaloniaList<Control>)(object)children)[i];
			VirtualizationInfo virtualizationInfo = FAItemsRepeater.TryGetVirtualizationInfo(val3);
			if (virtualizationInfo == null || !virtualizationInfo.IsHeldByLayout)
			{
				continue;
			}
			int index = virtualizationInfo.Index;
			if (index < clearedIndex)
			{
				if (index > num)
				{
					num = index;
					val2 = val3;
				}
			}
			else if (index >= clearedIndex && index < num2)
			{
				num2 = index;
				val = val3;
			}
		}
		Control val4 = null;
		if (val != null)
		{
			focusedChild = val;
			val4 = val;
		}
		if (val4 == null && val2 != null)
		{
			focusedChild = val2;
			val4 = val2;
		}
		return val4;
	}

	internal int GetElementIndex(VirtualizationInfo vInfo)
	{
		if (vInfo == null)
		{
			return -1;
		}
		if (!vInfo.IsRealized && !vInfo.IsInUniqueIdResetPool)
		{
			return -1;
		}
		return vInfo.Index;
	}

	internal void PrunePinnedElements()
	{
		EnsureEventSubscriptions();
		for (int i = 0; i < _pinnedPool.Count; i++)
		{
			PinnedElementInfo pinnedElementInfo = _pinnedPool[i];
			if (!pinnedElementInfo.VirtualizationInfo.IsPinned)
			{
				_pinnedPool.RemoveAt(i);
				i--;
				ClearElementToElementFactory(pinnedElementInfo.PinnedElement);
			}
		}
	}

	internal void UpdatePin(Control element, bool addPin)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		Visual visualParent = VisualExtensions.GetVisualParent((Visual)(object)element);
		Control val = element;
		while (visualParent != null)
		{
			if (visualParent is FAItemsRepeater fAItemsRepeater)
			{
				VirtualizationInfo virtualizationInfo = FAItemsRepeater.GetVirtualizationInfo(val);
				if (virtualizationInfo.IsRealized)
				{
					if (addPin)
					{
						virtualizationInfo.AddPin();
					}
					else if (virtualizationInfo.IsPinned && virtualizationInfo.RemovePin() == 0)
					{
						((Layoutable)fAItemsRepeater).InvalidateMeasure();
					}
				}
			}
			val = (Control)visualParent;
			visualParent = VisualExtensions.GetVisualParent((Visual)(object)val);
		}
	}

	internal void OnItemsSourceChanged(object _, NotifyCollectionChangedEventArgs args)
	{
		switch (args.Action)
		{
		case NotifyCollectionChangedAction.Add:
		{
			int newStartingIndex = args.NewStartingIndex;
			int count = args.NewItems.Count;
			EnsureFirstLastRealizedIndices();
			if (newStartingIndex <= _lastRealizedElementIndexHeldByLayout)
			{
				_lastRealizedElementIndexHeldByLayout += count;
				Controls children2 = ((Panel)_owner).Children;
				int count2 = ((AvaloniaList<Control>)(object)children2).Count;
				for (int j = 0; j < count2; j++)
				{
					Control val2 = ((AvaloniaList<Control>)(object)children2)[j];
					VirtualizationInfo virtualizationInfo2 = FAItemsRepeater.GetVirtualizationInfo(val2);
					int index = virtualizationInfo2.Index;
					if (virtualizationInfo2.IsRealized && index >= newStartingIndex)
					{
						UpdateElementIndex(val2, virtualizationInfo2, index + count);
					}
				}
				break;
			}
			for (int k = 0; k < _pinnedPool.Count; k++)
			{
				PinnedElementInfo pinnedElementInfo = _pinnedPool[k];
				VirtualizationInfo virtualizationInfo3 = pinnedElementInfo.VirtualizationInfo;
				int index2 = virtualizationInfo3.Index;
				if (virtualizationInfo3.IsPinned && index2 >= newStartingIndex)
				{
					UpdateElementIndex(pinnedElementInfo.PinnedElement, virtualizationInfo3, index2 + count);
				}
			}
			break;
		}
		case NotifyCollectionChangedAction.Replace:
		{
			int oldStartingIndex = args.OldStartingIndex;
			int newStartingIndex2 = args.NewStartingIndex;
			int count3 = args.OldItems.Count;
			int count4 = args.NewItems.Count;
			if (oldStartingIndex != newStartingIndex2)
			{
				throw new InvalidOperationException("Replace is only allowed with OldStartingIndex equals to NewStartingIndex.");
			}
			if (count3 == 0)
			{
				throw new InvalidOperationException("Replace notification with args.OldItemsCount value of 0 is not allowed. Use Insert action instead.");
			}
			if (count4 == 0)
			{
				throw new InvalidOperationException("Replace notification with args.NewItemCount value of 0 is not allowed. Use Remove action instead.");
			}
			int num = count4 - count3;
			if (num == 0)
			{
				break;
			}
			Controls children3 = ((Panel)_owner).Children;
			for (int l = 0; l < ((AvaloniaList<Control>)(object)children3).Count; l++)
			{
				Control val3 = ((AvaloniaList<Control>)(object)children3)[l];
				VirtualizationInfo virtualizationInfo4 = FAItemsRepeater.GetVirtualizationInfo(val3);
				int index3 = virtualizationInfo4.Index;
				if (virtualizationInfo4.IsRealized && index3 >= oldStartingIndex + count3)
				{
					UpdateElementIndex(val3, virtualizationInfo4, index3 + num);
				}
			}
			EnsureFirstLastRealizedIndices();
			_lastRealizedElementIndexHeldByLayout += num;
			break;
		}
		case NotifyCollectionChangedAction.Remove:
		{
			int oldStartingIndex2 = args.OldStartingIndex;
			int count5 = args.OldItems.Count;
			Controls children4 = ((Panel)_owner).Children;
			for (int m = 0; m < ((AvaloniaList<Control>)(object)children4).Count; m++)
			{
				Control val4 = ((AvaloniaList<Control>)(object)children4)[m];
				VirtualizationInfo virtualizationInfo5 = FAItemsRepeater.GetVirtualizationInfo(val4);
				int index4 = virtualizationInfo5.Index;
				if (virtualizationInfo5.IsRealized)
				{
					if (virtualizationInfo5.AutoRecycleCandidate && oldStartingIndex2 <= index4 && index4 < oldStartingIndex2 + count5)
					{
						_owner.ClearElementImpl(val4);
					}
					else if (index4 >= oldStartingIndex2 + count5)
					{
						UpdateElementIndex(val4, virtualizationInfo5, index4 - count5);
					}
				}
			}
			InvalidateRealizedIndicesHeldByLayout();
			break;
		}
		case NotifyCollectionChangedAction.Reset:
			if (!_isDataSourceStableResetPending)
			{
				if (_owner.ItemsSourceView.HasKeyIndexMapping)
				{
					_isDataSourceStableResetPending = true;
				}
				Controls children = ((Panel)_owner).Children;
				for (int i = 0; i < ((AvaloniaList<Control>)(object)children).Count; i++)
				{
					Control val = ((AvaloniaList<Control>)(object)children)[i];
					VirtualizationInfo virtualizationInfo = FAItemsRepeater.GetVirtualizationInfo(val);
					if (virtualizationInfo.IsRealized && virtualizationInfo.AutoRecycleCandidate)
					{
						_owner.ClearElementImpl(val);
					}
				}
			}
			InvalidateRealizedIndicesHeldByLayout();
			break;
		case NotifyCollectionChangedAction.Move:
			break;
		}
	}

	private void EnsureFirstLastRealizedIndices()
	{
		if (_firstRealizedElementIndexHeldByLayout == int.MaxValue)
		{
			GetElementIfAlreadyHeldByLayout(0);
		}
	}

	internal void OnLayoutChanging()
	{
		if (_owner.ItemsSourceView != null && _owner.ItemsSourceView.HasKeyIndexMapping)
		{
			_isDataSourceStableResetPending = true;
		}
	}

	internal void OnOwnerArranged()
	{
		if (!_isDataSourceStableResetPending)
		{
			return;
		}
		_isDataSourceStableResetPending = false;
		foreach (KeyValuePair<string, Control> item in _resetPool)
		{
			ClearElement(item.Value, isClearedDueToCollectionChange: true);
		}
		_resetPool.Clear();
		InvalidateRealizedIndicesHeldByLayout();
	}

	private Control GetElementIfAlreadyHeldByLayout(int index)
	{
		Control result = null;
		bool flag = _firstRealizedElementIndexHeldByLayout == int.MaxValue;
		bool flag2 = _firstRealizedElementIndexHeldByLayout <= index && index <= _lastRealizedElementIndexHeldByLayout;
		if (flag | flag2)
		{
			Controls children = ((Panel)_owner).Children;
			for (int i = 0; i < ((AvaloniaList<Control>)(object)children).Count; i++)
			{
				Control val = ((AvaloniaList<Control>)(object)children)[i];
				VirtualizationInfo virtualizationInfo = FAItemsRepeater.TryGetVirtualizationInfo(val);
				if (virtualizationInfo == null || !virtualizationInfo.IsHeldByLayout)
				{
					continue;
				}
				int index2 = virtualizationInfo.Index;
				_firstRealizedElementIndexHeldByLayout = Math.Min(_firstRealizedElementIndexHeldByLayout, index2);
				_lastRealizedElementIndexHeldByLayout = Math.Max(_lastRealizedElementIndexHeldByLayout, index2);
				if (virtualizationInfo.Index == index)
				{
					result = val;
					if (!flag)
					{
						break;
					}
				}
			}
		}
		return result;
	}

	private Control GetElementFromUniqueIdResetPool(int index)
	{
		Control val = null;
		if (_isDataSourceStableResetPending)
		{
			val = _resetPool.Remove(index);
			if (val != null)
			{
				VirtualizationInfo virtualizationInfo = FAItemsRepeater.GetVirtualizationInfo(val);
				virtualizationInfo.MoveOwnershipToLayoutFromUniqueIdResetPool();
				UpdateElementIndex(val, virtualizationInfo, index);
				_firstRealizedElementIndexHeldByLayout = Math.Max(_firstRealizedElementIndexHeldByLayout, index);
				_lastRealizedElementIndexHeldByLayout = Math.Max(_lastRealizedElementIndexHeldByLayout, index);
			}
		}
		return val;
	}

	private Control GetElementFromPinnedElements(int index)
	{
		Control result = null;
		for (int i = 0; i < _pinnedPool.Count; i++)
		{
			PinnedElementInfo pinnedElementInfo = _pinnedPool[i];
			if (pinnedElementInfo.VirtualizationInfo.Index == index)
			{
				_pinnedPool.RemoveAt(i);
				result = pinnedElementInfo.PinnedElement;
				pinnedElementInfo.VirtualizationInfo.MoveOwnershipToLayoutFromPinnedPool();
				_firstRealizedElementIndexHeldByLayout = Math.Min(_firstRealizedElementIndexHeldByLayout, index);
				_lastRealizedElementIndexHeldByLayout = Math.Max(_lastRealizedElementIndexHeldByLayout, index);
				break;
			}
		}
		return result;
	}

	private Control GetElementFromElementFactory(int index)
	{
		object at = _owner.ItemsSourceView.GetAt(index);
		Control val = null;
		IFAElementFactory providedElementFactory = _owner.ItemTemplateShim;
		if (providedElementFactory == null)
		{
			val = (Control)((at is Control) ? at : null);
		}
		if (val == null)
		{
			FAElementFactoryGetArgs elementFactoryGetArgs = _elementFactoryGetArgs;
			try
			{
				elementFactoryGetArgs.Data = at;
				elementFactoryGetArgs.Parent = (Control)(object)_owner;
				elementFactoryGetArgs.Index = index;
				val = GetElementFactory().GetElement(elementFactoryGetArgs);
			}
			finally
			{
				elementFactoryGetArgs.Data = null;
				elementFactoryGetArgs.Parent = null;
			}
		}
		VirtualizationInfo virtualizationInfo = FAItemsRepeater.TryGetVirtualizationInfo(val);
		if (virtualizationInfo == null)
		{
			virtualizationInfo = FAItemsRepeater.CreateAndInitializeVirtualizationInfo(val);
		}
		virtualizationInfo.MustClearDataContext = false;
		FAContainerContentChangingEventArgs e = null;
		bool shouldPhase = _owner.ShouldPhase;
		if (at != val && val != null)
		{
			object dataContext = null;
			Control val2 = (Control)((at is Control) ? at : null);
			if (val2 != null)
			{
				if (((StyledElement)val2).DataContext != null)
				{
					dataContext = ((StyledElement)val2).DataContext;
				}
			}
			else
			{
				dataContext = at;
			}
			((StyledElement)val).DataContext = dataContext;
			virtualizationInfo.MustClearDataContext = true;
			if ((at != val) & shouldPhase)
			{
				virtualizationInfo.UpdatePhasingInfo(at);
				e = new FAContainerContentChangingEventArgs(index, at, val, virtualizationInfo, 0, _phaser);
				_owner.RaiseContainerContentChanging(e);
			}
		}
		virtualizationInfo.MoveOwnershipToLayoutFromElementFactory(index, _owner.ItemsSourceView.HasKeyIndexMapping ? _owner.ItemsSourceView.KeyFromIndex(index) : string.Empty);
		FAItemsRepeater owner = _owner;
		Controls children = ((Panel)owner).Children;
		if ((object)VisualExtensions.GetVisualParent((Visual)(object)val) != owner)
		{
			((AvaloniaList<Control>)(object)children).Add(val);
		}
		owner.TransitionManager.OnElementPrepared(val);
		owner.OnElementPrepared(val, index, virtualizationInfo);
		_firstRealizedElementIndexHeldByLayout = Math.Min(_firstRealizedElementIndexHeldByLayout, index);
		_lastRealizedElementIndexHeldByLayout = Math.Max(_lastRealizedElementIndexHeldByLayout, index);
		return val;
		IFAElementFactory GetElementFactory()
		{
			if (providedElementFactory == null)
			{
				_owner.ItemTemplate = (IDataTemplate)(object)FuncDataTemplate.Default;
				return _owner.ItemTemplateShim;
			}
			return providedElementFactory;
		}
	}

	private bool ClearElementToUniqueIdResetPool(Control element, VirtualizationInfo virtInfo)
	{
		if (_isDataSourceStableResetPending)
		{
			_resetPool.Add(element);
			virtInfo.MoveOwnershipToUniqueIdResetPoolFromLayout();
		}
		return _isDataSourceStableResetPending;
	}

	private bool ClearElementToAnimator(Control element, VirtualizationInfo virtInfo)
	{
		bool num = _owner.TransitionManager.ClearElement(element);
		if (num)
		{
			int index = virtInfo.Index;
			virtInfo.MoveOwnershipToAnimator();
			if (_lastFocusedElement == element)
			{
				MoveFocusFromClearedIndex(index);
			}
		}
		return num;
	}

	private bool ClearElementToPinnedPool(Control element, VirtualizationInfo virtInfo, bool isClearedDueToCollectionChange)
	{
		int num;
		if (!isClearedDueToCollectionChange)
		{
			num = (virtInfo.IsPinned ? 1 : 0);
			if (num != 0)
			{
				_pinnedPool.Add(new PinnedElementInfo(element, virtInfo));
				virtInfo.MoveOwnershipToLayoutFromPinnedPool();
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private void UpdateFocusedElement()
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		FAItemsRepeater owner = _owner;
		TopLevel topLevel = TopLevel.GetTopLevel((Visual)(object)owner);
		Control val = null;
		Control val2 = null;
		if (topLevel != null)
		{
			IInputElement focusedElement = topLevel.FocusManager.GetFocusedElement();
			val = (Control)(object)((focusedElement is Control) ? focusedElement : null);
		}
		if (val != null)
		{
			for (Visual visualParent = VisualExtensions.GetVisualParent((Visual)(object)val); visualParent != null; visualParent = VisualExtensions.GetVisualParent((Visual)(object)val))
			{
				if (visualParent is FAItemsRepeater fAItemsRepeater)
				{
					if (val != null)
					{
						Control val3 = val;
						if (fAItemsRepeater == owner && FAItemsRepeater.GetVirtualizationInfo(val3).IsRealized)
						{
							val2 = val3;
						}
					}
					break;
				}
				val = (Control)visualParent;
			}
		}
		if (_lastFocusedElement != val2)
		{
			if (_lastFocusedElement != null)
			{
				UpdatePin(_lastFocusedElement, addPin: false);
			}
			if (val2 != null)
			{
				UpdatePin(val2, addPin: true);
			}
			_lastFocusedElement = val2;
		}
	}

	private void OnFocusChanged(object _, RoutedEventArgs __)
	{
		UpdateFocusedElement();
	}

	private void EnsureEventSubscriptions()
	{
		if (!_gotFocus)
		{
			_gotFocus = true;
			((InputElement)_owner).GotFocus += OnFocusChanged;
			((InputElement)_owner).LostFocus += OnFocusChanged;
		}
	}

	private void UpdateElementIndex(Control element, VirtualizationInfo virtInfo, int index)
	{
		int index2 = virtInfo.Index;
		if (index2 != index)
		{
			virtInfo.UpdateIndex(index);
			_owner.OnElementIndexChanged(element, index2, index);
		}
	}

	private void InvalidateRealizedIndicesHeldByLayout()
	{
		_firstRealizedElementIndexHeldByLayout = int.MaxValue;
		_lastRealizedElementIndexHeldByLayout = int.MinValue;
	}
}
