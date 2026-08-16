using System;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage;

/// <summary>
/// Starts the default app associated with the specified file or URI.
/// </summary>
public interface ILauncher
{
	/// <summary>
	/// Starts the default app associated with the URI scheme name for the specified URI.
	/// </summary>
	/// <param name="uri">The URI.</param>
	/// <returns>True, if launch operation was successful. False, if unsupported or failed.</returns>
	Task<bool> LaunchUriAsync(Uri uri);

	/// <summary>
	/// Starts the default app associated with the specified storage file or folder.
	/// </summary>
	/// <param name="storageItem">The file or folder.</param>
	/// <returns>True, if launch operation was successful. False, if unsupported or failed.</returns>
	Task<bool> LaunchFileAsync(IStorageItem storageItem);
}
