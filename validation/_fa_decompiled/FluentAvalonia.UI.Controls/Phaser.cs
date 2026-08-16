using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

internal class Phaser
{
	private readonly struct ElementInfo
	{
		[CompilerGenerated]
		private readonly Rect _003CLastArrangeBounds_003Ek__BackingField;

		public Control Element { get; }

		public int Phase { get; }

		public Rect LastArrangeBounds
		{
			[CompilerGenerated]
			get
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				return _003CLastArrangeBounds_003Ek__BackingField;
			}
		}

		public TypedEventHandler<FAItemsRepeater, FAContainerContentChangingEventArgs> Callback { get; }

		public VirtualizationInfo VirtInfo { get; }

		public ElementInfo(Control element, int phase, TypedEventHandler<FAItemsRepeater, FAContainerContentChangingEventArgs> callback, VirtualizationInfo virtInfo)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			Element = element;
			VirtInfo = virtInfo;
			Phase = phase;
			LastArrangeBounds = virtInfo.ArrangeBounds;
			Callback = callback;
		}
	}

	private readonly FAItemsRepeater _owner;

	private List<ElementInfo> _pendingElements;

	private bool _registeredForCallbacks;

	public Phaser(FAItemsRepeater owner)
	{
		_owner = owner;
	}

	public void PhaseElement(Control element, VirtualizationInfo virtInfo, FAContainerContentChangingEventArgs cArgs)
	{
		if (_pendingElements == null)
		{
			_pendingElements = new List<ElementInfo>();
		}
		_pendingElements.Insert(0, new ElementInfo(element, cArgs.Phase, cArgs.callback, virtInfo));
		RegisterForCallback();
	}

	public void StopPhasing(Control element, VirtualizationInfo virtInfo)
	{
		if (_pendingElements == null)
		{
			return;
		}
		for (int num = _pendingElements.Count - 1; num >= 0; num--)
		{
			if (_pendingElements[num].Element == element)
			{
				_pendingElements.RemoveAt(num);
			}
		}
		virtInfo.UpdatePhasingInfo(null);
	}

	public void DoPhasedWorkCallback()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		MarkCallbackReceived();
		if (_pendingElements != null && _pendingElements.Count > 0 && !BuildTreeScheduler.ShouldYield())
		{
			Rect visibleWindow = _owner.VisibleWindow;
			SortElements(visibleWindow);
			int num = _pendingElements.Count - 1;
			do
			{
				ElementInfo elementInfo = _pendingElements[num];
				Control element = elementInfo.Element;
				VirtualizationInfo virtInfo = elementInfo.VirtInfo;
				int index = virtInfo.Index;
				int phase = elementInfo.Phase;
				if (phase > 0)
				{
					FAContainerContentChangingEventArgs args = new FAContainerContentChangingEventArgs(index, virtInfo.Data, element, virtInfo, phase, this);
					elementInfo.Callback(_owner, args);
					int num2 = -1;
					ValidatePhaseOrdering(phase, num2);
					((Layoutable)element).Measure(LayoutInformation.GetPreviousMeasureConstraint((Layoutable)(object)element).Value);
					if (num2 > 0)
					{
						if (num == 0 || num2 > _pendingElements[num - 1].Phase)
						{
							num--;
						}
					}
					else
					{
						_pendingElements.RemoveAt(num);
						num--;
					}
				}
				else
				{
					_pendingElements.RemoveAt(num);
					num--;
				}
				int count = _pendingElements.Count;
				if (num == -1)
				{
					num = count - 1;
				}
				else if (num > -1 && num < count - 1 && !((Rect)(ref visibleWindow)).Intersects(_pendingElements[num].LastArrangeBounds) && ((Rect)(ref visibleWindow)).Intersects(_pendingElements[count - 1].LastArrangeBounds))
				{
					num = count - 1;
				}
			}
			while (_pendingElements.Count > 0 && !BuildTreeScheduler.ShouldYield());
		}
		if (_pendingElements.Count > 0)
		{
			RegisterForCallback();
		}
	}

	private void RegisterForCallback()
	{
		if (!_registeredForCallbacks)
		{
			_registeredForCallbacks = true;
			BuildTreeScheduler.RegisterWork(_pendingElements[_pendingElements.Count - 1].Phase, DoPhasedWorkCallback);
		}
	}

	private void MarkCallbackReceived()
	{
		_registeredForCallbacks = false;
	}

	private static void ValidatePhaseOrdering(int currentPhase, int nextPhase)
	{
		if (nextPhase > 0 && nextPhase <= currentPhase)
		{
			throw new InvalidOperationException("Phases are required to be monotonically increasing.");
		}
	}

	private void SortElements(Rect visibleWindow)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		_pendingElements.Sort(delegate(ElementInfo lhs, ElementInfo rhs)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			Rect lastArrangeBounds = lhs.LastArrangeBounds;
			bool flag = ((Rect)(ref visibleWindow)).Intersects(lastArrangeBounds);
			Rect lastArrangeBounds2 = rhs.LastArrangeBounds;
			bool flag2 = ((Rect)(ref visibleWindow)).Intersects(lastArrangeBounds2);
			if ((flag & flag2) || (!flag && !flag2))
			{
				return lhs.Phase.CompareTo(rhs.Phase);
			}
			return (!flag) ? 1 : 0;
		});
	}
}
