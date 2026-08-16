using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Avalonia.Input;

/// <summary>
/// Contains extension methods for <see cref="T:Avalonia.Input.IDataTransferItem" />.
/// </summary>
public static class DataTransferItemExtensions
{
	/// <summary>
	/// Gets whether a <see cref="T:Avalonia.Input.IDataTransferItem" /> supports a specific format.
	/// </summary>
	/// <param name="dataTransferItem">The <see cref="T:Avalonia.Input.IDataTransferItem" /> instance.</param>
	/// <param name="format">The format to check.</param>
	/// <returns>true if <paramref name="format" /> is supported, false otherwise.</returns>
	public static bool Contains(this IDataTransferItem dataTransferItem, DataFormat format)
	{
		IReadOnlyList<DataFormat> formats = dataTransferItem.Formats;
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
	/// Tries to get a value for a given format from a <see cref="T:Avalonia.Input.IDataTransferItem" />.
	/// </summary>
	/// <param name="dataTransferItem">The <see cref="T:Avalonia.Input.IDataTransferItem" /> instance.</param>
	/// <param name="format">The format to retrieve.</param>
	/// <returns>A value for <paramref name="format" />, or null if the format is not supported.</returns>
	public static T? TryGetValue<T>(this IDataTransferItem dataTransferItem, DataFormat<T> format) where T : class
	{
		return dataTransferItem.TryGetRaw(format) as T;
	}

	/// <summary>
	/// Returns a text, if available, from a <see cref="T:Avalonia.Input.IDataTransferItem" /> instance.
	/// </summary>
	/// <param name="dataTransferItem">The <see cref="T:Avalonia.Input.IDataTransferItem" /> instance.</param>
	/// <returns>A string, or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.Text" />.
	public static string? TryGetText(this IDataTransferItem dataTransferItem)
	{
		return dataTransferItem.TryGetValue(DataFormat.Text);
	}

	/// <summary>
	/// Returns a file, if available, from a <see cref="T:Avalonia.Input.IDataTransferItem" /> instance.
	/// </summary>
	/// <param name="dataTransferItem">The <see cref="T:Avalonia.Input.IDataTransferItem" /> instance.</param>
	/// <returns>An <see cref="T:Avalonia.Platform.Storage.IStorageItem" /> (file or folder), or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.File" />.
	public static IStorageItem? TryGetFile(this IDataTransferItem dataTransferItem)
	{
		return dataTransferItem.TryGetValue(DataFormat.File);
	}

	/// <summary>
	/// Returns a bitmap, if available, from a <see cref="T:Avalonia.Input.IDataTransferItem" /> instance.
	/// </summary>
	/// <param name="dataTransferItem">The <see cref="T:Avalonia.Input.IDataTransferItem" /> instance.</param>
	/// <returns>A <see cref="T:Avalonia.Media.Imaging.Bitmap" />, or null if the format isn't available.</returns>
	/// <seealso cref="P:Avalonia.Input.DataFormat.Bitmap" />.
	public static Bitmap? TryGetBitmap(this IDataTransferItem dataTransferItem)
	{
		return dataTransferItem.TryGetValue(DataFormat.Bitmap);
	}
}
