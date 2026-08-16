using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage;

internal class FallbackStorageProvider : IStorageProvider
{
	private Func<Task<IStorageProvider?>>[] _factories;

	private readonly List<IStorageProvider> _providers = new List<IStorageProvider>();

	private int _nextProviderFactory;

	public bool CanOpen => true;

	public bool CanSave => true;

	public bool CanPickFolder => true;

	public FallbackStorageProvider(Func<Task<IStorageProvider?>>[] factories)
	{
		_factories = factories;
	}

	/// <summary>
	/// Discards any cached providers and replaces the factory list. Useful when the
	/// underlying platform state has changed (e.g. wayland compositor reconnect) and
	/// previously-selected providers may no longer be appropriate.
	/// </summary>
	public void Reset(Func<Task<IStorageProvider?>>[] factories)
	{
		_factories = factories;
		_providers.Clear();
		_nextProviderFactory = 0;
	}

	private async IAsyncEnumerable<IStorageProvider> GetProviders()
	{
		foreach (IStorageProvider provider in _providers)
		{
			yield return provider;
		}
		while (_nextProviderFactory < _factories.Length)
		{
			IStorageProvider storageProvider = await _factories[_nextProviderFactory]();
			_nextProviderFactory++;
			if (storageProvider != null)
			{
				_providers.Add(storageProvider);
				yield return storageProvider;
			}
		}
	}

	private async Task<IStorageProvider> GetFor(Func<IStorageProvider, bool> filter)
	{
		await foreach (IStorageProvider provider in GetProviders())
		{
			if (filter(provider))
			{
				return provider;
			}
		}
		throw new IOException("Unable to select a suitable storage provider");
	}

	public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
	{
		return await (await GetFor((IStorageProvider p) => p.CanOpen)).OpenFilePickerAsync(options);
	}

	public async Task<OpenFilePickerResult> OpenFilePickerWithResultAsync(FilePickerOpenOptions options)
	{
		return await (await GetFor((IStorageProvider p) => p.CanOpen)).OpenFilePickerWithResultAsync(options);
	}

	public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
	{
		return await (await GetFor((IStorageProvider p) => p.CanSave)).SaveFilePickerAsync(options);
	}

	public async Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options)
	{
		return await (await GetFor((IStorageProvider p) => p.CanSave)).SaveFilePickerWithResultAsync(options);
	}

	public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
	{
		return await (await GetFor((IStorageProvider p) => p.CanPickFolder)).OpenFolderPickerAsync(options);
	}

	private async Task<TResult?> FirstNotNull<TArg, TResult>(TArg arg, Func<IStorageProvider, TArg, Task<TResult?>> cb) where TResult : class
	{
		await foreach (IStorageProvider provider in GetProviders())
		{
			TResult val = await cb(provider, arg);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	public Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
	{
		return FirstNotNull(bookmark, (IStorageProvider p, string a) => p.OpenFileBookmarkAsync(a));
	}

	public Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
	{
		return FirstNotNull(bookmark, (IStorageProvider p, string a) => p.OpenFolderBookmarkAsync(a));
	}

	public Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath)
	{
		return FirstNotNull(filePath, (IStorageProvider p, Uri a) => p.TryGetFileFromPathAsync(filePath));
	}

	public Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
	{
		return FirstNotNull(folderPath, (IStorageProvider p, Uri a) => p.TryGetFolderFromPathAsync(a));
	}

	public Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
	{
		return FirstNotNull(wellKnownFolder, (IStorageProvider p, WellKnownFolder a) => p.TryGetWellKnownFolderAsync(a));
	}
}
