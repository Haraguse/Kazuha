using System;
using System.Collections.Generic;

namespace Avalonia.Input;

/// <summary>
/// Wraps a <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> into a <see cref="T:Avalonia.Input.IDataTransfer" />.
/// </summary>
/// <param name="asyncDataTransfer">The async object to wrap.</param>
/// <remarks>Using this type should be a last resort!</remarks>
internal sealed class AsyncToSyncDataTransfer(IAsyncDataTransfer asyncDataTransfer) : IDataTransfer, IDisposable, IAsyncDataTransfer
{
	private readonly IAsyncDataTransfer _asyncDataTransfer = asyncDataTransfer;

	private AsyncToSyncDataTransferItem[]? _items;

	public IReadOnlyList<DataFormat> Formats => _asyncDataTransfer.Formats;

	public IReadOnlyList<AsyncToSyncDataTransferItem> Items => _items ?? (_items = ProvideItems());

	IReadOnlyList<IDataTransferItem> IDataTransfer.Items => Items;

	IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items => _asyncDataTransfer.Items;

	private AsyncToSyncDataTransferItem[] ProvideItems()
	{
		IReadOnlyList<IAsyncDataTransferItem> items = _asyncDataTransfer.Items;
		int count = items.Count;
		AsyncToSyncDataTransferItem[] array = new AsyncToSyncDataTransferItem[count];
		for (int i = 0; i < count; i++)
		{
			IAsyncDataTransferItem asyncDataTransferItem = items[i];
			array[i] = new AsyncToSyncDataTransferItem(asyncDataTransferItem);
		}
		return array;
	}

	public void Dispose()
	{
		_asyncDataTransfer.Dispose();
	}
}
