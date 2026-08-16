using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Avalonia.Input;

/// <summary>
/// Contains extension methods for <see cref="T:Avalonia.Input.IAsyncDataTransfer" />.
/// </summary>
public static class AsyncDataTransferExtensions
{
	internal static IDataTransfer ToSynchronous(this IAsyncDataTransfer asyncDataTransfer, string logArea)
	{
		if (asyncDataTransfer is IDataTransfer result)
		{
			return result;
		}
		Logger.TryGet(LogEventLevel.Warning, logArea)?.Log(null, "Using a synchronous wrapper for IAsyncDataTransferItem {Type}. Consider implementing IDataTransfer instead.", asyncDataTransfer.GetType());
		return new AsyncToSyncDataTransfer(asyncDataTransfer);
	}

	internal static IAsyncDataTransfer ToAsynchronous(this IDataTransfer dataTransfer)
	{
		return (dataTransfer as IAsyncDataTransfer) ?? new SyncToAsyncDataTransfer(dataTransfer);
	}

	/// <summary>
	/// Gets whether a <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> supports a specific format.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.</param>
	/// <param name="format">The format to check.</param>
	/// <returns>true if <paramref name="format" /> is supported, false otherwise.</returns>
	public static bool Contains(this IAsyncDataTransfer dataTransfer, DataFormat format)
	{
		IReadOnlyList<DataFormat> formats = dataTransfer.Formats;
		int count = formats.Count;
		for (int i = 0; i < count; i++)
		{
			if (format == formats[i])
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Gets the list of <see cref="T:Avalonia.Input.IAsyncDataTransferItem" /> contained in this object, filtered by a given format.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Some platforms (such as Windows and X11) may only support a single data item for all formats
	/// except <see cref="P:Avalonia.Input.DataFormat.File" />.
	/// </para>
	/// <para>Items returned by this property must stay valid until the <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> is disposed.</para>
	/// </remarks>
	public static IEnumerable<IAsyncDataTransferItem> GetItems(this IAsyncDataTransfer dataTransfer, DataFormat format)
	{
		IReadOnlyList<IAsyncDataTransferItem> items = dataTransfer.Items;
		int count = items.Count;
		int i = 0;
		while (i < count)
		{
			IAsyncDataTransferItem asyncDataTransferItem = items[i];
			if (asyncDataTransferItem.Contains(format))
			{
				yield return asyncDataTransferItem;
			}
			int num = i + 1;
			i = num;
		}
	}

	/// <summary>
	/// Tries to get a value for a given format from a <see cref="T:Avalonia.Input.IAsyncDataTransfer" />.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.</param>
	/// <param name="format">The format to retrieve.</param>
	/// <returns>A value for <paramref name="format" />, or null if the format is not supported.</returns>
	/// <remarks>
	/// If the <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> contains several items supporting <paramref name="format" />,
	/// the first matching one will be returned.
	/// </remarks>
	public static Task<T?> TryGetValueAsync<T>(this IAsyncDataTransfer dataTransfer, DataFormat<T> format) where T : class
	{
		IAsyncDataTransferItem asyncDataTransferItem = dataTransfer.GetItems(format).FirstOrDefault();
		if (asyncDataTransferItem == null)
		{
			return Task.FromResult<T>(null);
		}
		return asyncDataTransferItem.TryGetValueAsync(format);
	}

	/// <summary>
	/// Tries to get multiple values for a given format from a <see cref="T:Avalonia.Input.IAsyncDataTransfer" />.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.</param>
	/// <param name="format">The format to retrieve.</param>
	/// <returns>A list of values for <paramref name="format" />, or null if the format is not supported.</returns>
	public static async Task<T[]?> TryGetValuesAsync<T>(this IAsyncDataTransfer dataTransfer, DataFormat<T> format) where T : class
	{
		List<T> results = null;
		foreach (IAsyncDataTransferItem item in dataTransfer.GetItems(format))
		{
			T val = await item.TryGetValueAsync(format);
			if (val != null)
			{
				if (results == null)
				{
					results = new List<T>();
				}
				results.Add(val);
			}
		}
		return results?.ToArray();
	}

	/// <summary>
	/// Returns a text, if available, from a <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.</param>
	/// <returns>A string, or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.Text" />.
	public static Task<string?> TryGetTextAsync(this IAsyncDataTransfer dataTransfer)
	{
		return dataTransfer.TryGetValueAsync(DataFormat.Text);
	}

	/// <summary>
	/// Returns a file, if available, from a <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.</param>
	/// <returns>An <see cref="T:Avalonia.Platform.Storage.IStorageItem" /> (file or folder), or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.File" />.
	public static Task<IStorageItem?> TryGetFileAsync(this IAsyncDataTransfer dataTransfer)
	{
		return dataTransfer.TryGetValueAsync(DataFormat.File);
	}

	/// <summary>
	/// Returns a list of files, if available, from a <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.</param>
	/// <returns>An array of <see cref="T:Avalonia.Platform.Storage.IStorageItem" /> (files or folders), or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.File" />.
	public static Task<IStorageItem[]?> TryGetFilesAsync(this IAsyncDataTransfer dataTransfer)
	{
		return dataTransfer.TryGetValuesAsync(DataFormat.File);
	}

	/// <summary>
	/// Returns a bitmap, if available, from a <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IAsyncDataTransfer" /> instance.</param>
	/// <returns>A <see cref="T:Avalonia.Media.Imaging.Bitmap" />, or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.Bitmap" />.
	public static Task<Bitmap?> TryGetBitmapAsync(this IAsyncDataTransfer dataTransfer)
	{
		return dataTransfer.TryGetValueAsync(DataFormat.Bitmap);
	}
}
