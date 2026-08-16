using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// A mutable implementation of <see cref="T:Avalonia.Input.IDataTransfer" /> and <see cref="T:Avalonia.Input.IAsyncDataTransfer" />.
/// </summary>
/// <remarks>
/// While it also implements <see cref="T:Avalonia.Input.IAsyncDataTransfer" />, this class always returns data synchronously.
/// For advanced usages, consider implementing <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> directly.
/// </remarks>
public sealed class DataTransfer : IDataTransfer, IDisposable, IAsyncDataTransfer
{
	private readonly List<DataTransferItem> _items = new List<DataTransferItem>();

	private DataFormat[]? _formats;

	/// <inheritdoc cref="P:Avalonia.Input.IDataTransferItem.Formats" />
	public IReadOnlyList<DataFormat> Formats
	{
		get
		{
			return _formats ?? (_formats = GetFormatsCore());
			DataFormat[] GetFormatsCore()
			{
				return Items.SelectMany((DataTransferItem item) => item.Formats).Distinct().ToArray();
			}
		}
	}

	/// <summary>
	/// Gets a list of <see cref="T:Avalonia.Input.DataTransferItem" /> contained in this object.
	/// </summary>
	public IReadOnlyList<DataTransferItem> Items => _items;

	IReadOnlyList<IDataTransferItem> IDataTransfer.Items => Items;

	IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items => Items;

	/// <summary>
	/// Adds an existing <see cref="T:Avalonia.Input.DataTransferItem" /> to this object.
	/// </summary>
	/// <param name="item">The item to add.</param>
	public void Add(DataTransferItem item)
	{
		ThrowHelper.ThrowIfNull(item, "item");
		_formats = null;
		_items.Add(item);
	}

	void IDisposable.Dispose()
	{
	}
}
