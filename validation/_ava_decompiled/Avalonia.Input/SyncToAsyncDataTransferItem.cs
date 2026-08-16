using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input;

/// <summary>
/// Wraps a <see cref="T:Avalonia.Input.IDataTransferItem" /> into a <see cref="T:Avalonia.Input.IAsyncDataTransferItem" />.
/// </summary>
/// <param name="dataTransferItem">The sync item to wrap.</param>
internal sealed class SyncToAsyncDataTransferItem(IDataTransferItem dataTransferItem) : IDataTransferItem, IAsyncDataTransferItem
{
	public IReadOnlyList<DataFormat> Formats => dataTransferItem.Formats;

	public object? TryGetRaw(DataFormat format)
	{
		return dataTransferItem.TryGetRaw(format);
	}

	public Task<object?> TryGetRawAsync(DataFormat format)
	{
		return Task.FromResult(dataTransferItem.TryGetRaw(format));
	}
}
