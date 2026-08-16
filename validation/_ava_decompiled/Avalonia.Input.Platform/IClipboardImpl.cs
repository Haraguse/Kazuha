using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform;

/// <summary>
/// Represents a platform-specific implementation of the clipboard.
/// </summary>
[PrivateApi]
public interface IClipboardImpl
{
	/// <inheritdoc cref="M:Avalonia.Input.Platform.IClipboard.TryGetDataAsync" />
	Task<IAsyncDataTransfer?> TryGetDataAsync();

	/// <inheritdoc cref="M:Avalonia.Input.Platform.IClipboard.SetDataAsync(Avalonia.Input.IAsyncDataTransfer)" />
	Task SetDataAsync(IAsyncDataTransfer dataTransfer);

	/// <inheritdoc cref="M:Avalonia.Input.Platform.IClipboard.ClearAsync" />
	Task ClearAsync();
}
