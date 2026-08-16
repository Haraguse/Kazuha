using System.Threading.Tasks;

namespace Avalonia.Input.Platform;

/// <summary>
/// Implementation of <see cref="T:Avalonia.Input.Platform.IClipboard" />
/// </summary>
internal sealed class Clipboard(IClipboardImpl clipboardImpl) : IClipboard
{
	private readonly IClipboardImpl _clipboardImpl = clipboardImpl;

	private IAsyncDataTransfer? _lastDataTransfer;

	public Task ClearAsync()
	{
		_lastDataTransfer?.Dispose();
		_lastDataTransfer = null;
		return _clipboardImpl.ClearAsync();
	}

	public Task SetDataAsync(IAsyncDataTransfer? dataTransfer)
	{
		if (dataTransfer == null)
		{
			return ClearAsync();
		}
		if (_clipboardImpl is IOwnedClipboardImpl)
		{
			_lastDataTransfer = dataTransfer;
		}
		return _clipboardImpl.SetDataAsync(dataTransfer);
	}

	public Task FlushAsync()
	{
		if (!(_clipboardImpl is IFlushableClipboardImpl flushableClipboardImpl))
		{
			return Task.CompletedTask;
		}
		return flushableClipboardImpl.FlushAsync();
	}

	public Task<IAsyncDataTransfer?> TryGetDataAsync()
	{
		return _clipboardImpl.TryGetDataAsync();
	}

	public async Task<IAsyncDataTransfer?> TryGetInProcessDataAsync()
	{
		if (_lastDataTransfer == null || !(_clipboardImpl is IOwnedClipboardImpl ownedClipboardImpl))
		{
			return null;
		}
		if (!(await ownedClipboardImpl.IsCurrentOwnerAsync()))
		{
			_lastDataTransfer = null;
		}
		return _lastDataTransfer;
	}
}
