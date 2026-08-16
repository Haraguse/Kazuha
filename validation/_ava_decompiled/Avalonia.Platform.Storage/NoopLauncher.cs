using System;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage;

internal class NoopLauncher : ILauncher
{
	public Task<bool> LaunchUriAsync(Uri uri)
	{
		return Task.FromResult(result: false);
	}

	public Task<bool> LaunchFileAsync(IStorageItem storageItem)
	{
		return Task.FromResult(result: false);
	}
}
