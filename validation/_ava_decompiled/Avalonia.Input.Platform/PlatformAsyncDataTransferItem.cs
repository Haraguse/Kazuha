using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Abstract implementation of <see cref="T:Avalonia.Input.IAsyncDataTransferItem" /> used by platform implementations.
/// </summary>
/// <remarks>Use this class when the platform can only provide the underlying data asynchronously.</remarks>
internal abstract class PlatformAsyncDataTransferItem : IAsyncDataTransferItem
{
	private DataFormat[]? _formats;

	public DataFormat[] Formats => _formats ?? (_formats = ProvideFormats());

	IReadOnlyList<DataFormat> IAsyncDataTransferItem.Formats => Formats;

	protected abstract DataFormat[] ProvideFormats();

	public bool Contains(DataFormat format)
	{
		return Array.IndexOf(Formats, format) >= 0;
	}

	public Task<object?> TryGetRawAsync(DataFormat format)
	{
		if (!Contains(format))
		{
			return Task.FromResult<object>(null);
		}
		return TryGetRawCoreAsync(format);
	}

	protected abstract Task<object?> TryGetRawCoreAsync(DataFormat format);
}
