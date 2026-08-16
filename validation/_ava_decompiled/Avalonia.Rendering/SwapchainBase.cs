using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;

namespace Avalonia.Rendering;

/// <summary>
/// A helper class for composition-backed swapchains, should not be a public API yet
/// </summary>
internal abstract class SwapchainBase<TImage> : IAsyncDisposable where TImage : class, ISwapchainImage
{
	private readonly List<TImage> _pendingImages = new List<TImage>();

	protected ICompositionGpuInterop Interop { get; }

	protected CompositionDrawingSurface Target { get; }

	public SwapchainBase(ICompositionGpuInterop interop, CompositionDrawingSurface target)
	{
		Interop = interop;
		Target = target;
	}

	private static bool IsBroken(TImage image)
	{
		return image.LastPresent?.IsFaulted ?? false;
	}

	private static bool IsReady(TImage image)
	{
		if (image.LastPresent != null)
		{
			return image.LastPresent.Status == TaskStatus.RanToCompletion;
		}
		return true;
	}

	private TImage? CleanupAndFindNextImage(PixelSize size)
	{
		TImage val = null;
		bool flag = false;
		for (int num = _pendingImages.Count - 1; num > -1; num--)
		{
			TImage val2 = _pendingImages[num];
			bool flag2 = IsReady(val2);
			bool flag3 = val2.Size == size;
			if (IsBroken(val2) || (!flag3 & flag2))
			{
				val2.DisposeAsync();
				_pendingImages.RemoveAt(num);
			}
			if (flag3 & flag2)
			{
				if (val == null)
				{
					val = val2;
				}
				else
				{
					flag = true;
				}
			}
		}
		if (!flag)
		{
			return null;
		}
		return val;
	}

	protected abstract TImage CreateImage(PixelSize size);

	protected IDisposable BeginDrawCore(PixelSize size, out TImage image)
	{
		TImage img = CleanupAndFindNextImage(size) ?? CreateImage(size);
		img.BeginDraw();
		_pendingImages.Remove(img);
		image = img;
		return Disposable.Create(delegate
		{
			img.Present();
			_pendingImages.Add(img);
		});
	}

	public async ValueTask DisposeAsync()
	{
		foreach (TImage pendingImage in _pendingImages)
		{
			await pendingImage.DisposeAsync();
		}
	}
}
