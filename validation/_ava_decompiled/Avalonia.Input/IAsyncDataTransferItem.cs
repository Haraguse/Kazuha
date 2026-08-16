using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Input;

/// <summary>
/// Represent an item inside a <see cref="T:Avalonia.Input.IAsyncDataTransfer" />.
/// An item may support several formats and can return the value of a given format on demand.
/// </summary>
/// <seealso cref="T:Avalonia.Input.DataTransferItem" />
public interface IAsyncDataTransferItem
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
	/// To retrieve a typed value, use <see cref="M:Avalonia.Input.AsyncDataTransferItemExtensions.TryGetValueAsync``1(Avalonia.Input.IAsyncDataTransferItem,Avalonia.Input.DataFormat{``0})" />.
	/// </para>
	/// </remarks>
	/// <seealso cref="M:Avalonia.Input.AsyncDataTransferItemExtensions.TryGetValueAsync``1(Avalonia.Input.IAsyncDataTransferItem,Avalonia.Input.DataFormat{``0})" />
	/// <seealso cref="M:Avalonia.Input.AsyncDataTransferExtensions.TryGetValueAsync``1(Avalonia.Input.IAsyncDataTransfer,Avalonia.Input.DataFormat{``0})" />
	Task<object?> TryGetRawAsync(DataFormat format);
}
