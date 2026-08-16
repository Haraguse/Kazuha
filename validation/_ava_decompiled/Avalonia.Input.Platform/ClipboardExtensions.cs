using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Avalonia.Input.Platform;

/// <summary>
/// Contains extension methods related to <see cref="T:Avalonia.Input.Platform.IClipboard" />.
/// </summary>
public static class ClipboardExtensions
{
	/// <summary>
	/// Gets a list containing the formats currently available from the clipboard.
	/// </summary>
	/// <returns>A list of formats. It can be empty if the clipboard is empty.</returns>
	public static async Task<IReadOnlyList<DataFormat>> GetDataFormatsAsync(this IClipboard clipboard)
	{
		using IAsyncDataTransfer asyncDataTransfer = await clipboard.TryGetDataAsync();
		IReadOnlyList<DataFormat> result;
		if (asyncDataTransfer != null)
		{
			result = asyncDataTransfer.Formats;
		}
		else
		{
			IReadOnlyList<DataFormat> readOnlyList = Array.Empty<DataFormat>();
			result = readOnlyList;
		}
		return result;
	}

	/// <summary>
	/// Tries to get a value for a given format from the clipboard.
	/// </summary>
	/// <param name="clipboard">The <see cref="T:Avalonia.Input.Platform.IClipboard" /> instance.</param>
	/// <param name="format">The format to retrieve.</param>
	/// <returns>A value for <paramref name="format" />, or null if the format is not supported.</returns>
	/// <remarks>
	/// If the <see cref="T:Avalonia.Input.Platform.IClipboard" /> contains several items supporting <paramref name="format" />,
	/// the first matching one will be returned.
	/// </remarks>
	public static async Task<T?> TryGetValueAsync<T>(this IClipboard clipboard, DataFormat<T> format) where T : class
	{
		using IAsyncDataTransfer dataTransfer = await clipboard.TryGetDataAsync();
		if (dataTransfer == null)
		{
			return null;
		}
		return await dataTransfer.TryGetValueAsync(format).ConfigureAwait(continueOnCapturedContext: false);
	}

	/// <summary>
	/// Tries to get multiple values for a given format from the clipboard.
	/// </summary>
	/// <param name="clipboard">The <see cref="T:Avalonia.Input.Platform.IClipboard" /> instance.</param>
	/// <param name="format">The format to retrieve.</param>
	/// <returns>A list of values for <paramref name="format" />, or null if the format is not supported.</returns>
	public static async Task<T[]?> TryGetValuesAsync<T>(this IClipboard clipboard, DataFormat<T> format) where T : class
	{
		using IAsyncDataTransfer dataTransfer = await clipboard.TryGetDataAsync();
		if (dataTransfer == null)
		{
			return null;
		}
		return await dataTransfer.TryGetValuesAsync(format).ConfigureAwait(continueOnCapturedContext: false);
	}

	/// <summary>
	/// Places a single value on the clipboard in the specified format.
	/// </summary>
	/// <param name="clipboard">The clipboard instance.</param>
	/// <param name="format">The data format.</param>
	/// <param name="value">The value to place on the clipboard.</param>
	/// <remarks>
	/// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
	/// <para>
	/// If <paramref name="value" /> is null, nothing will get placed on the clipboard and this method
	/// will be equivalent to <see cref="M:Avalonia.Input.Platform.IClipboard.ClearAsync" />.
	/// </para>
	/// </remarks>
	public static Task SetValueAsync<T>(this IClipboard clipboard, DataFormat<T> format, T? value) where T : class
	{
		if (value == null)
		{
			return clipboard.ClearAsync();
		}
		DataTransfer dataTransfer = new DataTransfer();
		dataTransfer.Add(DataTransferItem.Create(format, value));
		return clipboard.SetDataAsync(dataTransfer);
	}

	/// <summary>
	/// Places multiple values on the clipboard in the specified format.
	/// </summary>
	/// <param name="clipboard">The clipboard instance.</param>
	/// <param name="format">The data format.</param>
	/// <param name="values">The values to place on the clipboard.</param>
	/// <remarks>
	/// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
	/// <para>
	/// If <paramref name="values" /> is null or empty, nothing will get placed on the clipboard and this method
	/// will be equivalent to <see cref="M:Avalonia.Input.Platform.IClipboard.ClearAsync" />.
	/// </para>
	/// </remarks>
	public static Task SetValuesAsync<T>(this IClipboard clipboard, DataFormat<T> format, IEnumerable<T>? values) where T : class
	{
		if (values == null)
		{
			return clipboard.ClearAsync();
		}
		DataTransfer dataTransfer = new DataTransfer();
		foreach (T value in values)
		{
			dataTransfer.Add(DataTransferItem.Create(format, value));
		}
		if (dataTransfer.Items.Count != 0)
		{
			return clipboard.SetDataAsync(dataTransfer);
		}
		return clipboard.ClearAsync();
	}

	/// <summary>
	/// Returns a text, if available, from the clipboard.
	/// </summary>
	/// <param name="clipboard">The clipboard instance.</param>
	/// <returns>A string, or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.Text" />
	public static Task<string?> TryGetTextAsync(this IClipboard clipboard)
	{
		return clipboard.TryGetValueAsync(DataFormat.Text);
	}

	/// <summary>
	/// Returns a file, if available, from the clipboard.
	/// </summary>
	/// <param name="clipboard">The clipboard instance.</param>
	/// <returns>An <see cref="T:Avalonia.Platform.Storage.IStorageItem" /> (file or folder), or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.File" />.
	public static Task<IStorageItem?> TryGetFileAsync(this IClipboard clipboard)
	{
		return clipboard.TryGetValueAsync(DataFormat.File);
	}

	/// <summary>
	/// Returns a list of files, if available, from the clipboard.
	/// </summary>
	/// <param name="clipboard">The clipboard instance.</param>
	/// <returns>An array of <see cref="T:Avalonia.Platform.Storage.IStorageItem" /> (files or folders), or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.File" />.
	public static Task<IStorageItem[]?> TryGetFilesAsync(this IClipboard clipboard)
	{
		return clipboard.TryGetValuesAsync(DataFormat.File);
	}

	/// <summary>
	/// Returns a bitmap, if available, from the clipboard.
	/// </summary>
	/// <param name="clipboard">The clipboard instance.</param>
	/// <returns>A <see cref="T:Avalonia.Media.Imaging.Bitmap" />, or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.Bitmap" />.
	public static Task<Bitmap?> TryGetBitmapAsync(this IClipboard clipboard)
	{
		return clipboard.TryGetValueAsync(DataFormat.Bitmap);
	}

	/// <summary>
	/// Places a text on the clipboard.
	/// </summary>
	/// <param name="clipboard">The clipboard instance.</param>
	/// <param name="text">The value to place on the clipboard.</param>
	/// <remarks>
	/// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
	/// <para>
	/// If <paramref name="text" /> is null, nothing will get placed on the clipboard and this method
	/// will be equivalent to <see cref="M:Avalonia.Input.Platform.IClipboard.ClearAsync" />.
	/// </para>
	/// </remarks>
	/// <seealso cref="P:Avalonia.Input.DataFormat.Text" />
	public static Task SetTextAsync(this IClipboard clipboard, string? text)
	{
		return clipboard.SetValueAsync(DataFormat.Text, text);
	}

	/// <summary>
	/// Places a file on the clipboard.
	/// </summary>
	/// <param name="clipboard">The clipboard instance.</param>
	/// <param name="file">The file to place on the clipboard.</param>
	/// <remarks>
	/// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
	/// <para>
	/// If <paramref name="file" /> is null, nothing will get placed on the clipboard and this method
	/// will be equivalent to <see cref="M:Avalonia.Input.Platform.IClipboard.ClearAsync" />.
	/// </para>
	/// </remarks>
	/// <seealso cref="P:Avalonia.Input.DataFormat.File" />
	public static Task SetFileAsync(this IClipboard clipboard, IStorageItem? file)
	{
		return clipboard.SetValueAsync(DataFormat.File, file);
	}

	/// <summary>
	/// Places a list of files on the clipboard.
	/// </summary>
	/// <param name="clipboard">The clipboard instance.</param>
	/// <param name="files">The files to place on the clipboard.</param>
	/// <remarks>
	/// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
	/// <para>
	/// If <paramref name="files" /> is null or empty, nothing will get placed on the clipboard and this method
	/// will be equivalent to <see cref="M:Avalonia.Input.Platform.IClipboard.ClearAsync" />.
	/// </para>
	/// </remarks>
	/// <seealso cref="P:Avalonia.Input.DataFormat.File" />
	public static Task SetFilesAsync(this IClipboard clipboard, IEnumerable<IStorageItem>? files)
	{
		return clipboard.SetValuesAsync(DataFormat.File, files);
	}

	/// <summary>
	/// Places a bitmap on the clipboard.
	/// </summary>
	/// <param name="clipboard">The clipboard instance.</param>
	/// <param name="bitmap">The bitmap to place on the clipboard.</param>
	/// <remarks>
	/// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
	/// <para>
	/// If <paramref name="bitmap" /> is null, nothing will get placed on the clipboard and this method
	/// will be equivalent to <see cref="M:Avalonia.Input.Platform.IClipboard.ClearAsync" />.
	/// </para>
	/// </remarks>
	/// <seealso cref="P:Avalonia.Input.DataFormat.Bitmap" />
	public static Task SetBitmapAsync(this IClipboard clipboard, Bitmap? bitmap)
	{
		return clipboard.SetValueAsync(DataFormat.Bitmap, bitmap);
	}
}
