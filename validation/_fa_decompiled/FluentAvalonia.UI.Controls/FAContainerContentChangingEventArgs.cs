using System;
using Avalonia.Controls;
using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

/// <summary>
/// Provides data for the <see cref="E:FluentAvalonia.UI.Controls.FAItemsRepeater.ContainerContentChanging" /> event.
/// </summary>
public class FAContainerContentChangingEventArgs : EventArgs
{
	internal TypedEventHandler<FAItemsRepeater, FAContainerContentChangingEventArgs> callback;

	private VirtualizationInfo _virtInfo;

	private readonly Phaser _phaser;

	/// <summary>
	/// Gets the data item associated with this container.
	/// </summary>
	public object Item { get; internal set; }

	/// <summary>
	/// Gets the UI container used to display the current data item.
	/// </summary>
	public Control ItemContainer { get; internal set; }

	/// <summary>
	/// Gets the index in the ItemsSource of the data item associated with this container.
	/// </summary>
	public int ItemIndex { get; internal set; }

	/// <summary>
	///
	/// </summary>
	public int Phase { get; private set; }

	internal FAContainerContentChangingEventArgs(int index, object item, Control container, VirtualizationInfo virtInfo)
	{
		ItemIndex = index;
		Item = item;
		ItemContainer = container;
		_virtInfo = virtInfo;
	}

	internal FAContainerContentChangingEventArgs(int index, object item, Control container, VirtualizationInfo virtInfo, int phase, Phaser phaser)
	{
		ItemIndex = index;
		Item = item;
		ItemContainer = container;
		_virtInfo = virtInfo;
		Phase = phase;
		_phaser = phaser;
	}

	public void RegisterUpdateCallback(TypedEventHandler<FAItemsRepeater, FAContainerContentChangingEventArgs> callback)
	{
		_phaser.PhaseElement(ItemContainer, _virtInfo, new FAContainerContentChangingEventArgs(ItemIndex, Item, ItemContainer, _virtInfo, Phase + 1, _phaser)
		{
			callback = callback
		});
	}
}
