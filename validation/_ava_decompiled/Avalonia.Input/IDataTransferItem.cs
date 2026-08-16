using System.Collections.Generic;

namespace Avalonia.Input;

/// <summary>
/// Represent an item inside a <see cref="T:Avalonia.Input.IDataTransfer" />.
/// An item may support several formats and can return the value of a given format on demand.
/// </summary>
/// <seealso cref="T:Avalonia.Input.DataTransferItem" />
public interface IDataTransferItem
{
	/// <summary>
	/// Gets the formats supported by this item.
	/// </summary>
	IReadOnlyList<DataFormat> Formats { get; }

	/// <summary>
	/// Tries to get a value for a given format.
	/// </summary>
	/// <param name="format">The format to retrieve.</param>
	/// <returns>A value for <paramref name="format" />, or null if the format is not supported.</returns>
	/// <remarks>
	/// <para>
	/// Implementations of this method are expected to return a value matching the exact type
	/// of the generic argument of the underlying <see cref="T:Avalonia.Input.DataFormat`1" />.
	/// </para>
	/// <para>
	/// To retrieve a typed value, use <see cref="M:Avalonia.Input.DataTransferItemExtensions.TryGetValue``1(Avalonia.Input.IDataTransferItem,Avalonia.Input.DataFormat{``0})" />.
	/// </para>
	/// </remarks>
	/// <seealso cref="M:Avalonia.Input.DataTransferItemExtensions.TryGetValue``1(Avalonia.Input.IDataTransferItem,Avalonia.Input.DataFormat{``0})" />
	/// <seealso cref="M:Avalonia.Input.DataTransferExtensions.TryGetValue``1(Avalonia.Input.IDataTransfer,Avalonia.Input.DataFormat{``0})" />
	object? TryGetRaw(DataFormat format);
}
