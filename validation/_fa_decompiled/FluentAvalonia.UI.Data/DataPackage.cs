using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace FluentAvalonia.UI.Data;

/// <summary>
/// Contains the data a user want to exchange
/// </summary>
public sealed class DataPackage : IDataTransfer, IDisposable, IAsyncDataTransfer
{
	[CompilerGenerated]
	private DragDropEffects _003CRequestedOperation_003Ek__BackingField;

	private readonly DataTransfer _dt;

	/// <summary>
	/// Gets or sets the requested operation for the data object
	/// </summary>
	public DragDropEffects RequestedOperation
	{
		[CompilerGenerated]
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _003CRequestedOperation_003Ek__BackingField;
		}
		[CompilerGenerated]
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			_003CRequestedOperation_003Ek__BackingField = value;
		}
	}

	IReadOnlyList<DataFormat> IDataTransfer.Formats => _dt.Formats;

	IReadOnlyList<IDataTransferItem> IDataTransfer.Items => (IReadOnlyList<IDataTransferItem>)_dt.Items;

	public IReadOnlyList<DataFormat> Formats => _dt.Formats;

	public IReadOnlyList<IAsyncDataTransferItem> Items => (IReadOnlyList<IAsyncDataTransferItem>)_dt.Items;

	public DataPackage()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		base._002Ector();
		_dt = new DataTransfer();
	}

	public void SetText(string text)
	{
		_dt.Add(DataTransferItem.CreateText(text));
	}

	public string GetText()
	{
		return DataTransferExtensions.TryGetText((IDataTransfer)(object)_dt);
	}

	public void SetStorageItems(IEnumerable<IStorageItem> items)
	{
		foreach (IStorageItem item in items)
		{
			_dt.Add(DataTransferItem.CreateFile(item));
		}
	}

	public IReadOnlyList<IStorageItem> GetStorageItems()
	{
		return DataTransferExtensions.TryGetFiles((IDataTransfer)(object)_dt);
	}

	public void SetBitmap(Bitmap bmp)
	{
		throw new NotImplementedException("Avalonia doesn't currently support this");
	}

	public void Dispose()
	{
	}
}
