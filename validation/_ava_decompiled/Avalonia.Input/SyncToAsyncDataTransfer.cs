using System;
using System.Collections.Generic;

namespace Avalonia.Input;

/// <summary>
/// Wraps a <see cref="T:Avalonia.Input.IDataTransfer" /> into a <see cref="T:Avalonia.Input.IAsyncDataTransfer" />.
/// </summary>
/// <param name="dataTransfer">The sync object to wrap.</param>
internal sealed class SyncToAsyncDataTransfer(IDataTransfer dataTransfer) : IDataTransfer, IDisposable, IAsyncDataTransfer
{
	private SyncToAsyncDataTransferItem[]? _items;

	public IReadOnlyList<DataFormat> Formats => dataTransfer.Formats;

	public IReadOnlyList<SyncToAsyncDataTransferItem> Items => _items ?? (_items = ProvideItems());

	IReadOnlyList<IDataTransferItem> IDataTransfer.Items => dataTransfer.Items;

	IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items => Items;

	private SyncToAsyncDataTransferItem[] ProvideItems()
	{
		IReadOnlyList<IDataTransferItem> items = dataTransfer.Items;
		int count = items.Count;
		SyncToAsyncDataTransferItem[] array = new SyncToAsyncDataTransferItem[count];
		for (int i = 0; i < count; i++)
		{
			IDataTransferItem dataTransferItem = items[i];
			array[i] = new SyncToAsyncDataTransferItem(dataTransferItem);
		}
		return array;
	}

	public void Dispose()
	{
		dataTransfer.Dispose();
	}
}
