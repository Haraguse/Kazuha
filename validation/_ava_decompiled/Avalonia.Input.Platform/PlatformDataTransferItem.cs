using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Abstract implementation of <see cref="T:Avalonia.Input.IDataTransferItem" /> used by platform implementations.
/// </summary>
/// <remarks>Use this class when the platform can only provide the underlying data synchronously.</remarks>
internal abstract class PlatformDataTransferItem : IDataTransferItem, IAsyncDataTransferItem
{
	private sealed class SingleFormatItem(DataFormat format, object value) : PlatformDataTransferItem
	{
		private readonly DataFormat _format = format;

		private readonly object _value = value;

		protected override DataFormat[] ProvideFormats()
		{
			return new DataFormat[1] { _format };
		}

		protected override object? TryGetRawCore(DataFormat format)
		{
			if (!_format.Equals(format))
			{
				return null;
			}
			return _value;
		}
	}

	private DataFormat[]? _formats;

	public DataFormat[] Formats => _formats ?? (_formats = ProvideFormats());

	IReadOnlyList<DataFormat> IDataTransferItem.Formats => Formats;

	IReadOnlyList<DataFormat> IAsyncDataTransferItem.Formats => Formats;

	protected abstract DataFormat[] ProvideFormats();

	public bool Contains(DataFormat format)
	{
		return Array.IndexOf(Formats, format) >= 0;
	}

	public object? TryGetRaw(DataFormat format)
	{
		if (!Contains(format))
		{
			return null;
		}
		return TryGetRawCore(format);
	}

	public Task<object?> TryGetRawAsync(DataFormat format)
	{
		if (!Contains(format))
		{
			return Task.FromResult<object>(null);
		}
		try
		{
			return Task.FromResult(TryGetRawCore(format));
		}
		catch (Exception exception)
		{
			return Task.FromException<object>(exception);
		}
	}

	protected abstract object? TryGetRawCore(DataFormat format);

	public static PlatformDataTransferItem Create<T>(DataFormat<T> format, T value) where T : class
	{
		return new SingleFormatItem(format, value);
	}
}
