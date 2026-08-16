using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Utilities;

namespace Avalonia.Input;

/// <summary>
/// A mutable implementation of <see cref="T:Avalonia.Input.IDataTransferItem" /> and <see cref="T:Avalonia.Input.IAsyncDataTransferItem" />.
/// This class also provides several static methods to easily create a <see cref="T:Avalonia.Input.DataTransferItem" /> for common usages.
/// </summary>
/// <remarks>
/// While it also implements <see cref="T:Avalonia.Input.IAsyncDataTransferItem" />, this class always returns data synchronously.
/// For advanced usages, consider implementing <see cref="T:Avalonia.Input.IAsyncDataTransferItem" /> directly.
/// </remarks>
public sealed class DataTransferItem : IDataTransferItem, IAsyncDataTransferItem
{
	private readonly struct DataAccessor(Func<object, object?> getValue, object state)
	{
		public object? GetValue()
		{
			return getValue(state);
		}
	}

	private Dictionary<DataFormat, DataAccessor>? _accessorByFormat;

	private KeyValuePair<DataFormat, DataAccessor>? _singleItem;

	private DataFormat[]? _formats;

	/// <inheritdoc cref="P:Avalonia.Input.IDataTransferItem.Formats" />
	public IReadOnlyList<DataFormat> Formats
	{
		get
		{
			return _formats ?? (_formats = ComputeFormats());
			DataFormat[] ComputeFormats()
			{
				if (_accessorByFormat != null)
				{
					return _accessorByFormat.Keys.ToArray();
				}
				KeyValuePair<DataFormat, DataAccessor>? singleItem = _singleItem;
				if (singleItem.HasValue)
				{
					KeyValuePair<DataFormat, DataAccessor> valueOrDefault = singleItem.GetValueOrDefault();
					return new DataFormat[1] { valueOrDefault.Key };
				}
				return Array.Empty<DataFormat>();
			}
		}
	}

	/// <inheritdoc />
	public object? TryGetRaw(DataFormat format)
	{
		return FindAccessor(format)?.GetValue();
	}

	Task<object?> IAsyncDataTransferItem.TryGetRawAsync(DataFormat format)
	{
		try
		{
			return Task.FromResult(TryGetRaw(format));
		}
		catch (Exception exception)
		{
			return Task.FromException<object>(exception);
		}
	}

	private DataAccessor? FindAccessor(DataFormat format)
	{
		if (_accessorByFormat != null)
		{
			if (!_accessorByFormat.TryGetValue(format, out var value))
			{
				return null;
			}
			return value;
		}
		KeyValuePair<DataFormat, DataAccessor>? singleItem = _singleItem;
		if (singleItem.HasValue)
		{
			KeyValuePair<DataFormat, DataAccessor> valueOrDefault = singleItem.GetValueOrDefault();
			if (valueOrDefault.Key.Equals(format))
			{
				return valueOrDefault.Value;
			}
		}
		return null;
	}

	/// <summary>
	/// Sets the value for a given format.
	/// </summary>
	/// <param name="format">The format.</param>
	/// <param name="value">
	/// The value corresponding to <paramref name="format" />.
	/// If null, the format won't be part of the <see cref="T:Avalonia.Input.DataTransferItem" />.
	/// </param>
	public void Set<T>(DataFormat<T> format, T? value) where T : class
	{
		ThrowHelper.ThrowIfNull(format, "format");
		if (value == null)
		{
			RemoveCore(format);
			return;
		}
		SetCore(format, new DataAccessor((object state) => state, value));
	}

	/// <summary>
	/// Sets a value created on demand for a given format.
	/// </summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="format">The format.</param>
	/// <param name="getValue">A function returning the value corresponding to <paramref name="format" />.</param>
	public void Set<T>(DataFormat<T> format, Func<T?> getValue) where T : class
	{
		ThrowHelper.ThrowIfNull(format, "format");
		ThrowHelper.ThrowIfNull(getValue, "getValue");
		SetCore(format, new DataAccessor((object state) => ((Func<T>)state)(), getValue));
	}

	private void SetCore(DataFormat format, DataAccessor accessor)
	{
		if (_accessorByFormat != null)
		{
			_accessorByFormat[format] = accessor;
		}
		else
		{
			KeyValuePair<DataFormat, DataAccessor>? singleItem = _singleItem;
			if (singleItem.HasValue)
			{
				KeyValuePair<DataFormat, DataAccessor> valueOrDefault = singleItem.GetValueOrDefault();
				if (!valueOrDefault.Key.Equals(format))
				{
					_accessorByFormat = new Dictionary<DataFormat, DataAccessor>
					{
						[valueOrDefault.Key] = valueOrDefault.Value,
						[format] = accessor
					};
					_singleItem = null;
					goto IL_0089;
				}
			}
			_singleItem = new KeyValuePair<DataFormat, DataAccessor>(format, accessor);
		}
		goto IL_0089;
		IL_0089:
		_formats = null;
	}

	private void RemoveCore(DataFormat format)
	{
		bool flag;
		if (_accessorByFormat != null)
		{
			flag = _accessorByFormat.Remove(format);
		}
		else
		{
			KeyValuePair<DataFormat, DataAccessor>? singleItem = _singleItem;
			if (singleItem.HasValue && singleItem.GetValueOrDefault().Key.Equals(format))
			{
				_singleItem = null;
				flag = true;
			}
			else
			{
				flag = false;
			}
		}
		if (flag)
		{
			_formats = null;
		}
	}

	/// <summary>
	/// Sets the value for the <see cref="P:Avalonia.Input.DataFormat.Text" /> format.
	/// </summary>
	/// <param name="value">
	/// The value corresponding to the <see cref="P:Avalonia.Input.DataFormat.Text" /> format.
	/// If null, the format won't be part of the <see cref="T:Avalonia.Input.DataTransferItem" />.
	/// </param>
	public void SetText(string? value)
	{
		Set(DataFormat.Text, value);
	}

	/// <summary>
	/// Sets the value for the <see cref="P:Avalonia.Input.DataFormat.File" /> format.
	/// </summary>
	/// <param name="value">
	/// The value corresponding to the <see cref="P:Avalonia.Input.DataFormat.File" /> format.
	/// If null, the format won't be part of the <see cref="T:Avalonia.Input.DataTransferItem" />.
	/// </param>
	public void SetFile(IStorageItem? value)
	{
		Set(DataFormat.File, value);
	}

	/// <summary>
	/// Sets the value for the <see cref="P:Avalonia.Input.DataFormat.Bitmap" /> format.
	/// </summary>
	/// <param name="value">
	/// The value corresponding to the <see cref="P:Avalonia.Input.DataFormat.Bitmap" /> format.
	/// If null, the format won't be part of the <see cref="T:Avalonia.Input.DataTransferItem" />.
	/// </param>
	public void SetBitmap(Bitmap? value)
	{
		Set(DataFormat.Bitmap, value);
	}

	/// <summary>
	/// Creates a new <see cref="T:Avalonia.Input.DataTransferItem" /> for a single format with a given value.
	/// </summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="format">The format.</param>
	/// <param name="value">
	/// The value corresponding to <paramref name="format" />.
	/// If null, the format won't be part of the <see cref="T:Avalonia.Input.DataTransferItem" />.
	/// </param>
	/// <returns>A <see cref="T:Avalonia.Input.DataTransferItem" /> instance.</returns>
	public static DataTransferItem Create<T>(DataFormat<T> format, T? value) where T : class
	{
		DataTransferItem dataTransferItem = new DataTransferItem();
		dataTransferItem.Set(format, value);
		return dataTransferItem;
	}

	/// <summary>
	/// Creates a new <see cref="T:Avalonia.Input.DataTransferItem" /> for a single format with a given value created on demand.
	/// </summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="format">The format.</param>
	/// <param name="getValue">A function returning the value corresponding to <paramref name="format" />.</param>
	/// <returns>A <see cref="T:Avalonia.Input.DataTransferItem" /> instance.</returns>
	public static DataTransferItem Create<T>(DataFormat<T> format, Func<T?> getValue) where T : class
	{
		DataTransferItem dataTransferItem = new DataTransferItem();
		dataTransferItem.Set(format, getValue);
		return dataTransferItem;
	}

	/// <summary>
	/// Creates a new <see cref="T:Avalonia.Input.DataTransferItem" /> with <see cref="P:Avalonia.Input.DataFormat.Text" /> as a single format.
	/// </summary>
	/// <param name="value">
	/// The value corresponding to the <see cref="P:Avalonia.Input.DataFormat.Text" /> format.
	/// If null, the format won't be part of the <see cref="T:Avalonia.Input.DataTransferItem" />.
	/// </param>
	/// <returns>A <see cref="T:Avalonia.Input.DataTransferItem" /> instance.</returns>
	public static DataTransferItem CreateText(string? value)
	{
		return Create(DataFormat.Text, value);
	}

	/// <summary>
	/// Creates a new <see cref="T:Avalonia.Input.DataTransferItem" /> with <see cref="P:Avalonia.Input.DataFormat.File" /> as a single format.
	/// </summary>
	/// <param name="value">
	/// The value corresponding to the <see cref="P:Avalonia.Input.DataFormat.File" /> format.
	/// If null, the format won't be part of the <see cref="T:Avalonia.Input.DataTransferItem" />.
	/// </param>
	/// <returns>A <see cref="T:Avalonia.Input.DataTransferItem" /> instance.</returns>
	public static DataTransferItem CreateFile(IStorageItem? value)
	{
		return Create(DataFormat.File, value);
	}
}
