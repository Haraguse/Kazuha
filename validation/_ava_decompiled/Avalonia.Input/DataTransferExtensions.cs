using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Avalonia.Input;

/// <summary>
/// Contains extension methods for <see cref="T:Avalonia.Input.IDataTransfer" />.
/// </summary>
public static class DataTransferExtensions
{
	/// <summary>
	/// Gets whether a <see cref="T:Avalonia.Input.IDataTransfer" /> supports a specific format.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IDataTransfer" /> instance.</param>
	/// <param name="format">The format to check.</param>
	/// <returns>true if <paramref name="format" /> is supported, false otherwise.</returns>
	public static bool Contains(this IDataTransfer dataTransfer, DataFormat format)
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
	/// Gets the list of <see cref="T:Avalonia.Input.IDataTransferItem" /> contained in this object, filtered by a given format.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Some platforms (such as Windows and X11) may only support a single data item for all formats
	/// except <see cref="P:Avalonia.Input.DataFormat.File" />.
	/// </para>
	/// <para>Items returned by this property must stay valid until the <see cref="T:Avalonia.Input.IDataTransfer" /> is disposed.</para>
	/// </remarks>
	public static IEnumerable<IDataTransferItem> GetItems(this IDataTransfer dataTransfer, DataFormat format)
	{
		IReadOnlyList<IDataTransferItem> items = dataTransfer.Items;
		int count = items.Count;
		int i = 0;
		while (i < count)
		{
			IDataTransferItem dataTransferItem = items[i];
			if (dataTransferItem.Contains(format))
			{
				yield return dataTransferItem;
			}
			int num = i + 1;
			i = num;
		}
	}

	/// <summary>
	/// Tries to get a value for a given format from a <see cref="T:Avalonia.Input.IDataTransfer" />.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IDataTransfer" /> instance.</param>
	/// <param name="format">The format to retrieve.</param>
	/// <returns>A value for <paramref name="format" />, or null if the format is not supported.</returns>
	/// <remarks>
	/// If the <see cref="T:Avalonia.Input.IDataTransfer" /> contains several items supporting <paramref name="format" />,
	/// the first matching one will be returned.
	/// </remarks>
	public static T? TryGetValue<T>(this IDataTransfer dataTransfer, DataFormat<T> format) where T : class
	{
		IDataTransferItem dataTransferItem = dataTransfer.GetItems(format).FirstOrDefault();
		if (dataTransferItem == null)
		{
			return null;
		}
		return dataTransferItem.TryGetValue(format);
	}

	/// <summary>
	/// Tries to get multiple values for a given format from a <see cref="T:Avalonia.Input.IDataTransfer" />.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IDataTransfer" /> instance.</param>
	/// <param name="format">The format to retrieve.</param>
	/// <returns>A list of values for <paramref name="format" />, or null if the format is not supported.</returns>
	public static T[]? TryGetValues<T>(this IDataTransfer dataTransfer, DataFormat<T> format) where T : class
	{
		List<T> list = null;
		foreach (IDataTransferItem item in dataTransfer.GetItems(format))
		{
			T val = item.TryGetValue(format);
			if (val != null)
			{
				if (list == null)
				{
					list = new List<T>();
				}
				list.Add(val);
			}
		}
		return list?.ToArray();
	}

	/// <summary>
	/// Returns a text, if available, from a <see cref="T:Avalonia.Input.IDataTransfer" /> instance.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IDataTransfer" /> instance.</param>
	/// <returns>A string, or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.Text" />.
	public static string? TryGetText(this IDataTransfer dataTransfer)
	{
		return dataTransfer.TryGetValue(DataFormat.Text);
	}

	/// <summary>
	/// Returns a file, if available, from a <see cref="T:Avalonia.Input.IDataTransfer" /> instance.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IDataTransfer" /> instance.</param>
	/// <returns>An <see cref="T:Avalonia.Platform.Storage.IStorageItem" /> (file or folder), or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.File" />.
	public static IStorageItem? TryGetFile(this IDataTransfer dataTransfer)
	{
		return dataTransfer.TryGetValue(DataFormat.File);
	}

	/// <summary>
	/// Returns a list of files, if available, from a <see cref="T:Avalonia.Input.IDataTransfer" /> instance.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IDataTransfer" /> instance.</param>
	/// <returns>An array of <see cref="T:Avalonia.Platform.Storage.IStorageItem" /> (files or folders), or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.File" />.
	public static IStorageItem[]? TryGetFiles(this IDataTransfer dataTransfer)
	{
		return dataTransfer.TryGetValues(DataFormat.File);
	}

	/// <summary>
	/// Returns a bitmap, if available, from a <see cref="T:Avalonia.Input.IDataTransfer" /> instance.
	/// </summary>
	/// <param name="dataTransfer">The <see cref="T:Avalonia.Input.IDataTransfer" /> instance.</param>
	/// <returns>A <see cref="T:Avalonia.Media.Imaging.Bitmap" />, or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.Bitmap" />.
	public static Bitmap? TryGetBitmap(this IDataTransfer dataTransfer)
	{
		return dataTransfer.TryGetValue(DataFormat.Bitmap);
	}
}
